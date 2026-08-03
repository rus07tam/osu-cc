using osu.Framework.Bindables;
using osu.Framework.Platform;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
        private int lastSave;

        /// <summary>True while <see cref="Reload"/> is re-applying persisted values.</summary>
        private bool reloading;

        public PluginSettings(Func<Storage?> storageProvider, string filename = "plugin.ini")
        {
            this.storageProvider = storageProvider;
            this.filename = filename;
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

                // Persist on every value change; the queue is deduplicated by lastSave.
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

            int current = Interlocked.Increment(ref lastSave);

            Task.Delay(100).ContinueWith(_ =>
            {
                if (current == lastSave)
                    Save();
            });
        }

        /// <summary>Writes the current values to the backing file. No-op until a storage is available.</summary>
        public bool Save()
        {
            var storage = storageProvider();
            if (storage == null)
                return false;

            lock (saveLock)
            {
                Interlocked.Increment(ref lastSave);

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
            Save();
        }
    }
}
