using osu.Framework.Bindables;
using osu.Framework.Platform;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Threading;

namespace osucc.Plugin
{
    /// <summary>
    /// String-keyed, ini-backed settings store for a single plugin, mirroring osu's own
    /// <see cref="osu.Framework.Configuration.IniConfigManager{TLookup}"/> pattern. Values are
    /// backed by <see cref="Bindable{T}"/>; storage is resolved lazily from the provided
    /// <paramref name="storageProvider"/>, so defaults can be registered during
    /// <see cref="IOsuCcPlugin.Load"/> (before the game exists) and persisted values loaded on
    /// <see cref="Reload"/> once the game attaches.
    /// </summary>
    public class PluginSettings : IDisposable
    {
        private readonly Func<Storage?> storageProvider;
        private readonly string filename;

        private readonly ConcurrentDictionary<string, IBindable> store = new();

        private readonly object saveLock = new();
        private readonly Timer saveTimer;
        private bool saveQueued;

        /// <summary>True while <see cref="Reload"/> is re-applying persisted values.</summary>
        private bool reloading;

        public PluginSettings(Func<Storage?> storageProvider, string filename = "plugin.ini")
        {
            this.storageProvider = storageProvider;
            this.filename = filename;

            // One-shot save timer, re-armed on every value change so writes coalesce into a
            // single save fired after the last change settles.
            saveTimer = new Timer(_ => flushSave(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            Reload();
        }

        /// <summary>
        /// Returns the config bindable for <paramref name="key"/>, creating it with the given
        /// default if needed. Mutations queue an asynchronous ini save.
        /// </summary>
        public Bindable<T> Bind<T>(string key, T defaultValue)
        {
            var existing = store.GetOrAdd(key, _ =>
            {
                var bindable = new Bindable<T>(defaultValue);

                // Persist on every value change; saves are coalesced by a single timer.
                bindable.ValueChanged += _ => queueSave();

                return bindable;
            });

            if (existing is not Bindable<T> typed)
                throw new InvalidCastException($"Settings key '{key}' is bound as {existing.GetType().Name}, not {typeof(Bindable<T>).Name}.");

            typed.Default = defaultValue;
            return typed;
        }

        /// <summary>Shortcut for <see cref="Bind{T}(string, T)"/> without keeping the bindable.</summary>
        public T Get<T>(string key) => Bind(key, default(T)!).Value;

        /// <summary>
        /// Returns the config bindable for an enum-typed key, creating it with the given default
        /// if needed. Keys persist under their numeric value, so renaming enum members never
        /// corrupts stored settings.
        /// </summary>
        public Bindable<T> Bind<TKey, T>(TKey key, T defaultValue)
            where TKey : struct, Enum
            => Bind(keyString(key), defaultValue);

        /// <summary>Shortcut for <see cref="Bind{TKey, T}(TKey, T)"/> without keeping the bindable.</summary>
        public T Get<TKey, T>(TKey key)
            where TKey : struct, Enum
            => Bind(keyString(key), default(T)!).Value;

        /// <summary>Sets the value of an enum-typed key.</summary>
        public void Set<TKey, T>(TKey key, T value)
            where TKey : struct, Enum
            => Bind(keyString(key), default(T)!).Value = value;

        /// <summary>Resets an enum-typed key back to the given default.</summary>
        public void Reset<TKey, T>(TKey key, T defaultValue)
            where TKey : struct, Enum
            => Bind(keyString(key), defaultValue).SetDefault();

        private static string keyString<TKey>(TKey key)
            where TKey : struct, Enum
            => Convert.ToInt64(key, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Reads a value straight from the backing file, bypassing the in-memory store. Unlike
        /// <see cref="Bind{T}(string, T)"/>, this sees keys that were never bound at
        /// <see cref="Reload"/> time — exactly what migration steps need to copy a legacy key.
        /// Returns <c>null</c> when the key is missing or storage is unavailable.
        /// </summary>
        public string? ReadPersisted(string key)
        {
            var storage = storageProvider();
            if (storage == null)
                return null;

            using (var stream = storage.GetStream(filename))
            {
                if (stream == null)
                    return null;

                using var reader = new StreamReader(stream);

                while (reader.ReadLine() is { } line)
                {
                    int equalsIndex = line.IndexOf('=');

                    if (line.Length == 0 || line[0] == '#' || equalsIndex < 0)
                        continue;

                    if (line[..equalsIndex].Trim() == key)
                        return line[(equalsIndex + 1)..].Trim();
                }
            }

            return null;
        }

        /// <summary>Whether the given settings key has been bound.</summary>
        public bool ContainsKey(string key) => store.ContainsKey(key);

        /// <summary>Removes a settings key; the next save no longer writes it. Useful for migration steps that rename a setting.</summary>
        public bool Remove(string key)
        {
            bool removed = store.TryRemove(key, out _);

            if (removed)
                queueSave();

            return removed;
        }

        /// <summary>
        /// Re-reads the backing file into the existing bindables (persisted values win). Called
        /// automatically when the game attaches; no-op until storage is available.
        /// </summary>
        public void Reload()
        {
            var storage = storageProvider();
            if (storage == null)
                return;

            using (var stream = storage.GetStream(filename))
            {
                if (stream == null)
                    return;

                using var reader = new StreamReader(stream);

                reloading = true;

                try
                {
                    while (reader.ReadLine() is { } line)
                    {
                        int equalsIndex = line.IndexOf('=');

                        if (line.Length == 0 || line[0] == '#' || equalsIndex < 0)
                            continue;

                        string key = line[..equalsIndex].Trim();
                        string value = line[(equalsIndex + 1)..].Trim();

                        if (store.TryGetValue(key, out IBindable? bindable) && bindable is IParseable parseable)
                        {
                            try
                            {
                                parseable.Parse(value, CultureInfo.InvariantCulture);
                            }
                            catch (Exception)
                            {
                                // keep the default on parse failure
                            }
                        }
                    }
                }
                finally
                {
                    reloading = false;
                }
            }
        }

        private void queueSave()
        {
            if (reloading)
                return;

            lock (saveLock)
            {
                if (saveQueued)
                    return;

                saveQueued = true;
                saveTimer.Change(100, Timeout.Infinite);
            }
        }

        private void flushSave()
        {
            bool shouldSave;

            lock (saveLock)
            {
                shouldSave = saveQueued;
                saveQueued = false;
            }

            if (shouldSave)
                Save();
        }

        /// <summary>Writes the current values to the backing file. No-op until a storage is available.</summary>
        public bool Save()
        {
            var storage = storageProvider();
            if (storage == null)
                return false;

            lock (saveLock)
            {
                try
                {
                    using (var stream = storage.CreateFileSafely(filename))
                    using (var writer = new StreamWriter(stream))
                    {
                        foreach (var pair in store)
                            writer.WriteLine($"{pair.Key} = {pair.Value.ToString()?.Replace("\n", "").Replace("\r", "")}");
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            saveTimer.Dispose();
            Save();
        }
    }
}
