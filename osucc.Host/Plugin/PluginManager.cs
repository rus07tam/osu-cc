using osu.Game.Overlays.Settings;
using osu.Game.Overlays.Toolbar;
using osucc.Client;
using osucc.Core;
using osucc.Localisation;
using osucc.Patches;
using System.Reflection;

namespace osucc.Plugin
{
    /// <summary>
    /// Discovers, loads and drives every plugin found in the osu-cc data folder's "plugins"
    /// directory. <see cref="PluginPackageStore"/> turns whatever is dropped there into the
    /// per-plugin layout (<c>&lt;root&gt;/&lt;id&gt;/</c>); <see cref="PluginStateStore"/>
    /// persists the per-plugin enabled state. Plugins are loaded via
    /// <see cref="Assembly.LoadFrom"/> into the default load context, so their osu.Game /
    /// osu.Framework / osucc references resolve to the already-loaded production assemblies.
    /// A plugin failing to load never takes the client down; it is logged and surfaced in
    /// the plugins overlay.
    /// </summary>
    public static class PluginManager
    {
        private static readonly object lockObject = new();

        private static readonly List<PluginEntry> plugins = new();
        private static readonly List<ToolbarButtonRegistration> toolbarButtonRegistrations = new();
        private static readonly Dictionary<string, Func<SettingsSubsection>> settingsSubsectionFactories = new(StringComparer.Ordinal);

        /// <summary>
        /// Every discovered plugin in the dependency-resolved load order (a dependency loads
        /// before its dependents; priority is only the tie-breaker). Kept separate from
        /// <see cref="plugins"/> (display order, sorted by priority); used for attach order.
        /// </summary>
        private static PluginEntry[] loadOrder = Array.Empty<PluginEntry>();

        // Plugin-id -> exported API objects. A plugin may export several distinct contract types.
        private static readonly Dictionary<string, List<object>> pluginApis = new();

        private static bool loadAttempted;
        private static bool attached;

        public static bool LiveReloadEnabled { get; private set; }

        private static string? pluginsDirectory;

        private static string PluginStatesPath => Path.Combine(PluginsDirectory, "plugin-states.ini");

        /// <summary>All discovered plugins, sorted by <see cref="OsuCcPluginAttribute.Priority"/>.</summary>
        public static IReadOnlyList<PluginEntry> Plugins
        {
            get
            {
                lock (lockObject)
                    return plugins.ToArray();
            }
        }

        /// <summary>
        /// Counts how many discovered plugins loaded successfully versus failed to load.
        /// Disabled plugins are skipped entirely.
        /// </summary>
        public static (int Loaded, int Failed) GetLoadSummary()
        {
            lock (lockObject)
            {
                int loaded = plugins.Count(p => p.Enabled && p.Loaded);
                int failed = plugins.Count(p => p.LoadError != null);
                return (loaded, failed);
            }
        }

        /// <summary>Toolbar button registrations made by plugins (consumed by the Toolbar.load postfix).</summary>
        public static IReadOnlyList<ToolbarButtonRegistration> ToolbarButtonRegistrations
        {
            get
            {
                lock (lockObject)
                    return toolbarButtonRegistrations.ToArray();
            }
        }

        /// <summary>Settings subsection factories registered by plugins, keyed by plugin id (invoked when the plugin manager builds a card's settings).</summary>
        public static IReadOnlyDictionary<string, Func<SettingsSubsection>> SettingsSubsectionFactories
        {
            get
            {
                lock (lockObject)
                    return new Dictionary<string, Func<SettingsSubsection>>(settingsSubsectionFactories, StringComparer.Ordinal);
            }
        }

        /// <summary>Returns the settings subsection factory registered by the given plugin id, or <c>null</c>.</summary>
        internal static Func<SettingsSubsection>? GetSettingsSubsectionFactory(string pluginId)
        {
            lock (lockObject)
            {
                return settingsSubsectionFactories.TryGetValue(pluginId, out var factory) ? factory : null;
            }
        }

        /// <summary>
        /// Path of the folder scanned for plugin archives / plugin folders: the game data
        /// folder's <c>osu-cc/plugins</c> directory (see <see cref="PluginDirectories"/>).
        /// Resolved once and cached.
        /// </summary>
        public static string PluginsDirectory
        {
            get
            {
                lock (lockObject)
                    return pluginsDirectory ??= PluginDirectories.ResolvePluginsDirectory();
            }
        }

        /// <summary>Whether the given plugin id is enabled (defaults to enabled when unset).</summary>
        public static bool IsEnabled(string id)
        {
            lock (lockObject)
                return PluginStateStore.IsEnabled(id);
        }

        /// <summary>
        /// Persists the enabled state of a plugin. Disabled plugins are skipped on the next
        /// launch (they are still listed in the plugins overlay so they can be re-enabled).
        /// Once the game is attached, the change also applies immediately on the update thread:
        /// disabling revokes the plugin's patches/registrations and disposes it, enabling
        /// re-instantiates it from the retained type and attaches it.
        /// </summary>
        public static void SetPluginEnabled(string id, bool enabled)
        {
            PluginEntry? entry;
            bool gameAttached;

            lock (lockObject)
            {
                PluginStateStore.SetEnabled(id, enabled);
                entry = plugins.FirstOrDefault(p => p.Id == id);
                gameAttached = attached;

                if (entry != null)
                    entry.Enabled = enabled;
            }

            // Before the game attaches or if live reloading is disabled, the change is staged for next launch.
            if (!gameAttached || entry == null || !LiveReloadEnabled)
                return;

            // Must run on the update thread (the overlay toggle already is); mutates the live
            // game tree (drawable removal in plugin Dispose) and Harmony patch state.
            if (enabled)
                enablePlugin(entry);
            else
                disablePlugin(entry);
        }

        /// <summary>Re-instantiates a disabled plugin from its retained type and attaches it. Runs on the update thread.</summary>
        private static void enablePlugin(PluginEntry entry)
        {
            if (entry.Loaded)
                return;

            if (entry.ApiVersion != OsuCcPluginAttribute.CurrentApiVersion || entry.PluginType == null)
            {
                entry.LoadError = new NotSupportedException($"plugin API v{entry.ApiVersion} is not supported (current: v{OsuCcPluginAttribute.CurrentApiVersion})");
                TimingLog.Error($"PluginManager: '{entry.Name}' cannot be enabled: {entry.LoadError.Message}");
                return;
            }

            try
            {
                var instance = (OsuCcPlugin)Activator.CreateInstance(entry.PluginType)!;
                var host = new PluginHost(entry);

                entry.Plugin = instance;
                entry.Host = host;
                entry.LoadError = null;

                instance.Load(host);

                // Dependencies were attached at startup or in their own enable call; attach now.
                attachEntry(entry);

                entry.InitialEnabled = true;

                TimingLog.Info($"PluginManager: '{entry.Name}' enabled at runtime");
            }
            catch (Exception ex)
            {
                entry.Plugin = null;
                entry.Host = null;
                entry.Attached = false;
                entry.LoadError = ex;
                entry.AddDiagnostic(PluginDiagnostic.Error("Failed to enable plugin at runtime", ex, source: PluginDiagnosticSource.Lifecycle));

                TimingLog.Error($"PluginManager: '{entry.Name}' failed to enable: {ex}");
            }
        }

        /// <summary>Reverts a running plugin: unpatch + revoke registrations, then dispose it. Runs on the update thread.</summary>
        private static void disablePlugin(PluginEntry entry)
        {
            if (entry.Plugin == null)
                return;

            string name = entry.Name;

            // Stop the game from calling into plugin code first (unpatch), then let the plugin
            // tear down whatever it inserted into the live tree.
            try
            {
                (entry.Host as PluginHost)?.DisposeRuntime();
            }
            catch (Exception ex)
            {
                entry.AddDiagnostic(PluginDiagnostic.Error("DisposeRuntime failed", ex, source: PluginDiagnosticSource.Lifecycle));
                TimingLog.Error($"PluginManager: '{name}' DisposeRuntime failed: {ex}");
            }

            try
            {
                entry.Plugin.Dispose();
            }
            catch (Exception ex)
            {
                entry.AddDiagnostic(PluginDiagnostic.Error("Plugin Dispose() failed", ex, source: PluginDiagnosticSource.Lifecycle));
                TimingLog.Error($"PluginManager: '{name}' Dispose failed: {ex}");
            }

            List<string> dependents;
            lock (lockObject)
            {
                entry.Plugin = null;
                entry.Host = null;
                entry.Attached = false;
                entry.InitialEnabled = false;

                // Only still-running plugins count as dependents: a plugin that was itself
                // disabled no longer calls into this one, so it must not block the disable.
                dependents = plugins
                             .Where(p => p != entry && p.Loaded && p.Dependencies.Contains(entry.Id, StringComparer.Ordinal))
                             .Select(p => p.Name)
                             .ToList();
            }

            TimingLog.Info($"PluginManager: '{name}' disabled at runtime");

            // Exported APIs stay alive so already-loaded dependents do not crash; surface the gap.
            if (dependents.Count > 0)
            {
                string dependentNames = string.Join(", ", dependents);
                TimingLog.Error($"PluginManager: '{name}' disabled while still depended on by: {dependentNames}");
                ClientNotifications.Warning($"\"{name}\" was disabled while still depended on by: {dependentNames}");
            }
        }

        /// <summary>
        /// Scans the plugins folder, loads every <c>[OsuCcPlugin]</c> type and calls
        /// <see cref="OsuCcPlugin.Load"/>. Invoked from the startup hook right after the
        /// built-in patches are installed. Idempotent.
        /// </summary>
        public static void LoadAll()
        {
            lock (lockObject)
            {
                if (loadAttempted)
                    return;

                loadAttempted = true;
            }

            TimingLog.Info($"PluginManager: scanning {PluginsDirectory}");

            if (!Directory.Exists(PluginsDirectory))
            {
                TimingLog.Info("PluginManager: no plugins directory found; nothing to load");
                return;
            }

            PluginStateStore.Initialise(PluginStatesPath);
            PluginStateStore.Load();

            // Folders of deleted plugins are removed before anything is loaded (the dlls are
            // still unlocked, on Windows a loaded dll cannot be deleted) and before Prepare,
            // so a freshly dropped archive still re-installs the plugin.
            removePendingDeletes();

            PluginPackageStore.Prepare(PluginsDirectory);

            // Discovery: collect every [OsuCcPlugin] type without running any plugin code.
            // Staging payloads sit one level deeper (.staging/<archive>/); they hold the
            // freshly extracted payload, so discover them FIRST and then skip any regular
            // folder dll that has a same-named staged copy; the stale copy must not load at
            // all (its side effects, toolbar buttons, Harmony patches, would otherwise
            // register twice on the same launch).
            string stagingRoot = Path.Combine(PluginsDirectory, PluginPackageStore.StagingDirectoryName);
            var stagedDllNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var candidates = new List<PluginCandidate>();

            if (Directory.Exists(stagingRoot))
            {
                foreach (string stagedFolder in Directory.GetDirectories(stagingRoot))
                {
                    foreach (string dll in Directory.GetFiles(stagedFolder, "*.dll", SearchOption.AllDirectories))
                    {
                        stagedDllNames.Add(Path.GetFileName(dll));
                        discoverPluginDll(dll, candidates);
                    }
                }
            }

            foreach (string pluginFolder in Directory.GetDirectories(PluginsDirectory))
            {
                if (PluginPackageStore.IsUnderStaging(pluginFolder, PluginsDirectory))
                    continue;

                foreach (string dll in Directory.GetFiles(pluginFolder, "*.dll", SearchOption.AllDirectories))
                {
                    if (stagedDllNames.Contains(Path.GetFileName(dll)))
                    {
                        TimingLog.Info($"PluginManager: '{Path.GetFileName(dll)}' staged copy supersedes folder copy; skipping");
                        continue;
                    }

                    discoverPluginDll(dll, candidates);
                }
            }

            // Load in dependency-resolved order: a declared dependency always loads first,
            // otherwise the priority order (persisted override wins over the attribute) holds.
            var resolution = PluginDependencyResolver.Resolve(candidates);

            foreach (string warning in resolution.Warnings)
                TimingLog.Info(warning);

            foreach (var candidate in resolution.Order)
                instantiatePlugin(candidate);

            // Staging is transient; move every plugin payload into its id-folder now that
            // the ids are known, so the next launch loads straight from plugins/{id}/.
            PluginPackageStore.Finalize(Plugins, PluginsDirectory);

            lock (lockObject)
            {
                // Snapshot the dependency-resolved order before the list is re-sorted by
                // priority for display; AttachAllToGame follows this snapshot.
                loadOrder = plugins.ToArray();

                plugins.Sort((a, b) =>
                {
                    int byPriority = a.Priority.CompareTo(b.Priority);
                    return byPriority != 0 ? byPriority : string.CompareOrdinal(a.Name, b.Name);
                });
            }

            TimingLog.Info($"PluginManager: {Plugins.Count} plugin(s) discovered");
        }

        /// <summary>
        /// Removes the payload folders of pending-delete plugins and forgets their persisted state
        /// (only for folders that are actually gone).
        /// </summary>
        private static void removePendingDeletes()
        {
            var deletedIds = PluginStateStore.DeletedIds.ToArray();

            if (deletedIds.Length == 0)
                return;

            var removedIds = PluginPackageStore.RemovePendingDeletes(PluginsDirectory, deletedIds);

            foreach (string id in removedIds)
                PluginStateStore.Remove(id);
        }

        /// <summary>
        /// Builds a <see cref="PluginEntry"/> from the plugin metadata (the build-emitted
        /// attribute) and the discovered plugin type.
        /// </summary>
        private static PluginEntry createEntry(OsuCcPluginAttribute attribute, string directory, string? iconPath, Type? pluginType, IEnumerable<PluginDiagnostic>? initialDiagnostics = null)
        {
            bool isEnabled = PluginStateStore.IsEnabled(attribute.Id);

            var entry = new PluginEntry
            {
                Id = attribute.Id,
                Name = attribute.Name,
                Authors = resolveAuthors(attribute),
                Tags = attribute.Tags,
                Description = attribute.Description,
                Repository = string.IsNullOrWhiteSpace(attribute.Repository) ? null : attribute.Repository,
                Version = attribute.Version,
                IconResource = attribute.IconResource,
                Icon = attribute.Icon,
                Priority = PluginStateStore.GetPriority(attribute.Id) ?? attribute.Priority,
                ApiVersion = attribute.ApiVersion,
                Directory = directory,
                IconPath = iconPath,
                Dependencies = attribute.DependsOn,
                DependencyDeclarations = attribute.ResolveDependencyDeclarations(),
                Documents = resolveDocuments(attribute),
                PluginType = pluginType,
                Enabled = isEnabled,
                InitialEnabled = isEnabled,
            };

            if (initialDiagnostics != null)
            {
                foreach (var diag in initialDiagnostics)
                    entry.AddDiagnostic(diag);
            }

            return entry;
        }

        private static PluginDocument[] resolveDocuments(OsuCcPluginAttribute attribute)
        {
            if (attribute.DocumentPaths == null || attribute.DocumentPaths.Length == 0)
                return Array.Empty<PluginDocument>();

            var docs = new PluginDocument[attribute.DocumentPaths.Length];

            for (int i = 0; i < attribute.DocumentPaths.Length; i++)
            {
                string path = attribute.DocumentPaths[i];
                string title = attribute.DocumentTitles != null && i < attribute.DocumentTitles.Length && !string.IsNullOrWhiteSpace(attribute.DocumentTitles[i])
                    ? attribute.DocumentTitles[i]
                    : Path.GetFileNameWithoutExtension(path);
                string? icon = attribute.DocumentIcons != null && i < attribute.DocumentIcons.Length && !string.IsNullOrWhiteSpace(attribute.DocumentIcons[i])
                    ? attribute.DocumentIcons[i]
                    : null;

                docs[i] = new PluginDocument
                {
                    Title = title,
                    Path = path,
                    IconGlyph = icon,
                };
            }

            return docs;
        }

        /// <summary>
        /// Maps the attribute's author declaration to a list of <see cref="PluginAuthor"/>.
        /// </summary>
        private static PluginAuthor[] resolveAuthors(OsuCcPluginAttribute attribute)
        {
            int[]? ids = attribute.AuthorOsuIds;

            var authors = new PluginAuthor[attribute.AuthorNames.Length];

            for (int i = 0; i < attribute.AuthorNames.Length; i++)
            {
                int id = ids != null && i < ids.Length ? ids[i] : -1;
                authors[i] = id > 0
                    ? new PluginAuthor(attribute.AuthorNames[i], id)
                    : new PluginAuthor(attribute.AuthorNames[i]);
            }

            return authors;
        }

        /// <summary>
        /// Registers an entry, de-duplicating by id. During a scheme migration the same
        /// plugin may be discoverable twice: once in a stale folder and once in the fresh
        /// staging folder; the staging copy (new payload) wins and replaces the old one.
        /// </summary>
        private static void addEntry(PluginEntry entry)
        {
            lock (lockObject)
            {
                int existingIndex = plugins.FindIndex(p => p.Id == entry.Id);

                if (existingIndex >= 0)
                {
                    var existing = plugins[existingIndex];

                    bool entryStaged = PluginPackageStore.IsUnderStaging(entry.Directory, PluginsDirectory);
                    bool existingStaged = PluginPackageStore.IsUnderStaging(existing.Directory, PluginsDirectory);

                    if (entryStaged == existingStaged)
                    {
                        TimingLog.Info($"PluginManager: duplicate '{entry.Name}' (id '{entry.Id}') ignored");
                        return;
                    }

                    // The fresh payload lives in staging; it supersedes the stale folder copy.
                    if (!entryStaged)
                    {
                        TimingLog.Info($"PluginManager: '{entry.Name}' stale folder copy ignored (staging is newer)");
                        return;
                    }

                    plugins.RemoveAt(existingIndex);
                    TimingLog.Info($"PluginManager: '{entry.Name}' replaced stale folder copy with staged payload");
                }

                plugins.Add(entry);
            }
        }

        /// <summary>
        /// Discovers a plugin in a dll without running any plugin code; the type is instantiated
        /// later, in priority order. Plugins carry an assembly-level
        /// <see cref="OsuCcPluginAttribute"/> emitted by the build from the project file (exactly
        /// one plugin type per assembly).
        /// </summary>
        private static void discoverPluginDll(string path, List<PluginCandidate> candidates)
        {
            string fallbackId = Path.GetFileNameWithoutExtension(path);
            string pluginDirectory = Path.GetDirectoryName(path) ?? string.Empty;

            try
            {
                var assembly = Assembly.LoadFrom(path);
                OsuCcLocalisation.RegisterAssembly(assembly);

                var manifest = assembly.GetCustomAttribute<OsuCcPluginAttribute>();

                if (manifest == null)
                {
                    TimingLog.Info($"PluginManager: no [OsuCcPlugin] assembly manifest found in {path}");
                    return;
                }

                var pluginTypes = getLoadableTypes(assembly).Where(isPluginType).ToArray();

                if (pluginTypes.Length == 0)
                {
                    TimingLog.Info($"PluginManager: '{manifest.Name}' declares a manifest but no OsuCcPlugin type was found in {path}");
                    return;
                }

                if (pluginTypes.Length > 1)
                {
                    TimingLog.Error($"PluginManager: '{manifest.Name}' (id '{manifest.Id}') declares an assembly manifest but contains {pluginTypes.Length} plugin types; only one plugin per assembly is supported, skipping {path}");
                    return;
                }

                candidates.Add(new PluginCandidate(pluginTypes[0], manifest, pluginDirectory, resolveDeclaredIcon(manifest, pluginDirectory)));
            }
            catch (BadImageFormatException)
            {
                TimingLog.Info($"PluginManager: ignoring native or invalid assembly {path}");
            }
            catch (Exception ex)
            {
                var errorEntry = new PluginEntry
                {
                    Id = fallbackId,
                    Name = fallbackId,
                    Directory = pluginDirectory,
                    LoadError = ex,
                };
                errorEntry.AddDiagnostic(PluginDiagnostic.Error($"Failed to load assembly {Path.GetFileName(path)}", ex, source: PluginDiagnosticSource.Lifecycle));
                addEntry(errorEntry);

                TimingLog.Error($"PluginManager: failed to load assembly {path}: {ex}");
            }
        }

        private static bool isPluginType(Type type)
            => !type.IsNested && !type.IsAbstract && !type.IsInterface && typeof(OsuCcPlugin).IsAssignableFrom(type);

        /// <summary>
        /// Resolves the plugin's image icon: the manifest's declared <c>IconPath</c> (relative to
        /// the plugin folder).
        /// </summary>
        private static string? resolveDeclaredIcon(OsuCcPluginAttribute manifest, string pluginDirectory)
        {
            string? iconPath = null;

            if (!string.IsNullOrEmpty(manifest.IconPath))
            {
                string declared = Path.Combine(pluginDirectory, manifest.IconPath.Replace('\\', '/'));

                if (File.Exists(declared))
                {
                    iconPath = declared;
                }
                else
                {
                    TimingLog.Info($"PluginManager: '{manifest.Name}' declares icon '{manifest.IconPath}' but it is missing from the plugin folder");
                }
            }

            TimingLog.Info($"PluginManager: '{manifest.Name}' folder icon: {iconPath ?? "(none)"}");
            return iconPath;
        }

        /// <summary>Instantiates and loads a single discovered plugin type, recording the result as an entry.</summary>
        private static void instantiatePlugin(PluginCandidate candidate)
        {
            string id = candidate.Metadata.Id;

            if (!IsEnabled(id))
            {
                var disabledEntry = createEntry(candidate.Metadata, candidate.Directory, candidate.IconPath, candidate.Type, candidate.Diagnostics);
                disabledEntry.Enabled = false;

                addEntry(disabledEntry);

                TimingLog.Info($"PluginManager: '{candidate.Metadata.Name}' is disabled; skipping");
                return;
            }

            if (candidate.IsBlocked || candidate.Diagnostics.Any(d => d.Level == PluginDiagnosticLevel.Error))
            {
                var blockedEntry = createEntry(candidate.Metadata, candidate.Directory, candidate.IconPath, candidate.Type, candidate.Diagnostics);
                addEntry(blockedEntry);

                TimingLog.Error($"PluginManager: '{candidate.Metadata.Name}' blocked from loading due to dependency errors");
                return;
            }

            if (candidate.Metadata.ApiVersion != OsuCcPluginAttribute.CurrentApiVersion)
            {
                var versionEntry = createEntry(candidate.Metadata, candidate.Directory, candidate.IconPath, candidate.Type, candidate.Diagnostics);
                versionEntry.LoadError = new NotSupportedException($"plugin API v{candidate.Metadata.ApiVersion} is not supported (current: v{OsuCcPluginAttribute.CurrentApiVersion})");
                versionEntry.AddDiagnostic(PluginDiagnostic.Error($"plugin API v{candidate.Metadata.ApiVersion} is not supported (current: v{OsuCcPluginAttribute.CurrentApiVersion})", versionEntry.LoadError, source: PluginDiagnosticSource.Lifecycle));

                addEntry(versionEntry);

                TimingLog.Error($"PluginManager: '{candidate.Metadata.Name}' skipped: {versionEntry.LoadError.Message}");
                return;
            }

            try
            {
                var instance = (OsuCcPlugin)Activator.CreateInstance(candidate.Type)!;
                var entry = createEntry(candidate.Metadata, candidate.Directory, candidate.IconPath, candidate.Type, candidate.Diagnostics);
                entry.Plugin = instance;

                var host = new PluginHost(entry);
                entry.Host = host;

                instance.Load(host);

                addEntry(entry);

                TimingLog.Info($"PluginManager: loaded '{entry.Name}' v{entry.Version} ({candidate.Directory})");
            }
            catch (Exception ex)
            {
                var failedEntry = createEntry(candidate.Metadata, candidate.Directory, candidate.IconPath, candidate.Type, candidate.Diagnostics);
                failedEntry.LoadError = ex;
                failedEntry.AddDiagnostic(PluginDiagnostic.Error("Plugin Load() threw an exception", ex, source: PluginDiagnosticSource.Lifecycle));

                addEntry(failedEntry);

                TimingLog.Error($"PluginManager: '{candidate.Metadata.Name}' failed to load: {ex}");
            }
        }

        /// <summary>
        /// Marks a plugin for deletion. The payload folder is removed on the next launch (the
        /// loaded dll cannot be deleted mid-session); until then the plugin stays loaded but is
        /// flagged as <see cref="PluginEntry.PendingDelete"/>. Before deletion is persisted, the
        /// plugin's <see cref="IPluginLifecycle.OnUninstall"/> hook runs in-place (outside the
        /// manager lock, so it can call back into the host).
        /// </summary>
        public static void RemovePlugin(string id)
        {
            PluginEntry? entry;

            lock (lockObject)
                entry = plugins.FirstOrDefault(p => p.Id == id);

            if (entry?.Plugin != null)
            {
                try
                {
                    entry.Plugin.OnUninstall();
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"PluginManager: '{entry.Name}' OnUninstall failed: {ex}");
                }
            }

            lock (lockObject)
            {
                PluginStateStore.MarkDeleted(id);

                if (entry != null)
                    entry.PendingDelete = true;
            }
        }

        /// <summary>
        /// Cancels a pending deletion (the delete button on the details card): unmarks the plugin
        /// in <see cref="PluginStateStore"/> and clears the in-memory <see cref="PluginEntry.PendingDelete"/>
        /// flag so the plugin is fully interactive again.
        /// </summary>
        public static void RestorePlugin(string id)
        {
            lock (lockObject)
            {
                PluginStateStore.UnmarkDeleted(id);

                if (plugins.FirstOrDefault(p => p.Id == id) is { } entry)
                    entry.PendingDelete = false;
            }
        }

        /// <summary>
        /// Clears a plugin's data: its persisted settings (<c>plugin.ini</c>), its
        /// <c>osu-cc/data/&lt;id&gt;</c> folder and the recorded version/schema tracking (so the
        /// plugin's migrations re-run on the next launch). The plugin itself stays installed and
        /// enabled; the reset becomes visible on the next launch once the plugin reads its storage
        /// again. Safe to call before the game attaches (storage may be unavailable, then it is a
        /// no-op).
        /// </summary>
        public static void ClearPluginData(string id)
        {
            var gameStorage = Reflection.GetStorage(ClientApi.Game);

            if (gameStorage == null)
                return;

            try
            {
                var settingsStorage = gameStorage.GetStorageForDirectory($"osu-cc/plugins/{id}");

                if (settingsStorage.Exists("plugin.ini"))
                    settingsStorage.Delete("plugin.ini");

                var dataRoot = gameStorage.GetStorageForDirectory("osu-cc/data");

                if (dataRoot.ExistsDirectory(id))
                    dataRoot.DeleteDirectory(id);

                PluginStateStore.ClearData(id);
                TimingLog.Info($"PluginManager: cleared data for '{id}' (applies on next launch)");
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PluginManager: failed to clear data for '{id}': {ex}");
            }
        }

        /// <summary>
        /// Re-orders plugins by the given id sequence (up/down arrows in the overlay): position
        /// <c>i</c> becomes priority <c>i</c>, persisted per plugin. Affects the overlay order and
        /// the load/attach order on the next launch.
        /// </summary>
        public static void SetPluginOrder(IReadOnlyList<string> orderedIds)
        {
            lock (lockObject)
            {
                var order = new Dictionary<string, int>();

                for (int i = 0; i < orderedIds.Count; i++)
                    order[orderedIds[i]] = i;

                PluginStateStore.SetPriorities(order);

                foreach (var entry in plugins)
                {
                    if (order.TryGetValue(entry.Id, out int priority))
                        entry.Priority = priority;
                }

                plugins.Sort((a, b) =>
                {
                    int byPriority = a.Priority.CompareTo(b.Priority);
                    return byPriority != 0 ? byPriority : string.CompareOrdinal(a.Name, b.Name);
                });
            }
        }

        /// <summary>
        /// Called from <see cref="osucc.Client.ClientBootstrap.AttachToGame"/> once the game instance,
        /// storage and dependencies are available. Reloads persisted settings from disk and calls
        /// <see cref="OsuCcPlugin.AttachToGame"/> on every loaded plugin (update thread).
        /// </summary>
        public static void AttachAllToGame()
        {
            lock (lockObject)
            {
                if (attached)
                    return;

                attached = true;
                LiveReloadEnabled = ClientConfig.LivePluginReloading.Value;
            }

            TimingLog.Info($"PluginManager: live plugin reloading enabled = {LiveReloadEnabled}");

            foreach (var entry in loadOrder)
                attachEntry(entry);
        }

        /// <summary>Reloads settings, runs migrations and calls <see cref="OsuCcPlugin.AttachToGame"/> for one plugin. Runs on the update thread.</summary>
        private static void attachEntry(PluginEntry entry)
        {
            if (!entry.Loaded || entry.Plugin == null)
                return;

            try
            {
                (entry.Host as PluginHost)?.ReloadSettings();

                // Migrations run before AttachToGame so the plugin always reads current-schema data.
                bool freshInstall = PluginStateStore.GetVersion(entry.Id) == null;
                runMigrations(entry, freshInstall);

                entry.Plugin.AttachToGame();
                entry.Attached = true;

                dispatchLifecycle(entry, freshInstall);
                TimingLog.Info($"PluginManager: '{entry.Name}' attached to game");
            }
            catch (Exception ex)
            {
                entry.AddDiagnostic(PluginDiagnostic.Error("AttachToGame() failed", ex, source: PluginDiagnosticSource.Lifecycle));
                TimingLog.Error($"PluginManager: '{entry.Name}' AttachToGame failed: {ex}");
            }
        }

        /// <summary>
        /// Applies the plugin's data migrations when its persisted schema version is below
        /// <see cref="IPluginMigrations.SchemaVersion"/>. Each step's result is persisted before
        /// the next runs, so a crash mid-sequence resumes from the last applied schema. Fresh
        /// installs have nothing to migrate; their data is born up to date.
        /// </summary>
        private static void runMigrations(PluginEntry entry, bool freshInstall)
        {
            if (entry.Host == null || entry.Plugin == null)
                return;

            int currentSchema = entry.Plugin.SchemaVersion;

            if (freshInstall)
            {
                if (PluginStateStore.GetSchemaVersion(entry.Id) != currentSchema)
                    PluginStateStore.SetSchemaVersion(entry.Id, currentSchema);

                return;
            }

            int start = PluginStateStore.GetSchemaVersion(entry.Id) ?? 0;

            if (start > currentSchema)
            {
                TimingLog.Error($"PluginManager: '{entry.Name}' data schema {start} is newer than plugin schema {currentSchema}; skipping migrations");
                return;
            }

            if (start == currentSchema)
                return;

            var seen = new HashSet<int>();

            foreach (IPluginMigration step in entry.Plugin.Migrations
                     .Where(m => m.ToVersion > start && m.ToVersion <= currentSchema)
                     .OrderBy(m => m.ToVersion))
            {
                if (!seen.Add(step.ToVersion))
                {
                    TimingLog.Error($"PluginManager: '{entry.Name}' duplicate migration target v{step.ToVersion} ignored");
                    continue;
                }

                try
                {
                    step.Apply(entry.Host.GetSettings(), message => PluginLog.Write(entry.Id, LogLevel.Info, message));
                    PluginStateStore.SetSchemaVersion(entry.Id, step.ToVersion);
                    TimingLog.Info($"PluginManager: '{entry.Name}' applied data migration -> schema v{step.ToVersion}");
                }
                catch (Exception ex)
                {
                    entry.AddDiagnostic(PluginDiagnostic.Error($"Data migration to schema v{step.ToVersion} failed", ex, source: PluginDiagnosticSource.Lifecycle));
                    TimingLog.Error($"PluginManager: '{entry.Name}' data migration to v{step.ToVersion} failed: {ex}");
                    return;
                }
            }
        }

        /// <summary>
        /// Fires the plugin's lifecycle hooks once the plugin is attached: <c>OnInstall</c> on a
        /// fresh install, <c>OnUpdate</c> when the loaded version differs from the last recorded
        /// one. The version record is persisted after the hook succeeds; the record is written for
        /// every loaded plugin, so a plugin that adopts lifecycle later never
        /// fires a spurious install.
        /// </summary>
        private static void dispatchLifecycle(PluginEntry entry, bool freshInstall)
        {
            string currentVersion = entry.Version;

            try
            {
                if (freshInstall)
                {
                    entry.Plugin?.OnInstall();

                    PluginStateStore.SetVersion(entry.Id, currentVersion);
                    TimingLog.Info($"PluginManager: '{entry.Name}' installed (v{currentVersion})");
                }
                else
                {
                    string? previousVersion = PluginStateStore.GetVersion(entry.Id);

                    if (previousVersion != null && !versionsEqual(previousVersion, currentVersion))
                    {
                        entry.Plugin?.OnUpdate(previousVersion);

                        PluginStateStore.SetVersion(entry.Id, currentVersion);
                        TimingLog.Info($"PluginManager: '{entry.Name}' updated {previousVersion} -> {currentVersion}");
                    }
                }
            }
            catch (Exception ex)
            {
                entry.AddDiagnostic(PluginDiagnostic.Error("Lifecycle hook (OnInstall/OnUpdate) failed", ex, source: PluginDiagnosticSource.Lifecycle));
                TimingLog.Error($"PluginManager: '{entry.Name}' lifecycle hook failed: {ex}");
            }
        }

        /// <summary>Compares two version strings, preferring a numeric comparison when both parse as <see cref="Version"/>.</summary>
        private static bool versionsEqual(string a, string b)
        {
            if (Version.TryParse(a, out Version? parsedA) && Version.TryParse(b, out Version? parsedB))
                return parsedA == parsedB;

            return string.Equals(a, b, StringComparison.Ordinal);
        }

        internal static IDisposable RegisterToolbarButton(Func<ToolbarButton> factory, ToolbarButtonPlacement placement, float? layoutPosition)
        {
            var registration = new ToolbarButtonRegistration(factory, placement, layoutPosition);

            lock (lockObject)
                toolbarButtonRegistrations.Add(registration);

            // The toolbar is built once at startup; a button registered afterwards (live enable)
            // must be injected immediately. No-op while the toolbar has not loaded yet.
            ToolbarLoadPatch.AddPluginButton(registration);

            return new ToolbarButtonLifecycleHandle(registration);
        }

        internal static IDisposable RegisterSettingsSubsection(string pluginId, Func<SettingsSubsection> factory)
        {
            lock (lockObject)
                settingsSubsectionFactories[pluginId] = factory;

            return new SettingsSubsectionLifecycleHandle(pluginId, factory);
        }

        /// <summary>Revokes a toolbar button registration when disposed.</summary>
        private sealed class ToolbarButtonLifecycleHandle : IDisposable
        {
            private readonly ToolbarButtonRegistration registration;
            private bool disposed;

            public ToolbarButtonLifecycleHandle(ToolbarButtonRegistration registration)
                => this.registration = registration;

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;

                lock (lockObject)
                    toolbarButtonRegistrations.Remove(registration);

                // The toolbar is not rebuilt when a plugin is disabled, so the button drawn at
                // load time (or on a live enable) must be removed from the live toolbar now.
                // Runs outside the lock: removing a drawable mutates the live tree, which must
                // never re-enter PluginManager while the manager lock is held.
                registration.RemoveCreated();
            }
        }

        /// <summary>Revokes a settings subsection registration when disposed.</summary>
        private sealed class SettingsSubsectionLifecycleHandle : IDisposable
        {
            private readonly string pluginId;
            private readonly Func<SettingsSubsection> factory;
            private bool disposed;

            public SettingsSubsectionLifecycleHandle(string pluginId, Func<SettingsSubsection> factory)
                => (this.pluginId, this.factory) = (pluginId, factory);

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;

                lock (lockObject)
                {
                    // Only remove the key if it still points at our factory, so a re-registration
                    // after this handle was created is not torn down by it.
                    if (settingsSubsectionFactories.TryGetValue(pluginId, out var current) && ReferenceEquals(current, factory))
                        settingsSubsectionFactories.Remove(pluginId);
                }
            }
        }

        /// <summary>Registers an exported API object under the given plugin id (see <see cref="IOsuCcPluginHost.ExportApi"/>).</summary>
        internal static void ExportPluginApi(string pluginId, object api)
        {
            lock (lockObject)
            {
                if (!pluginApis.TryGetValue(pluginId, out var apis))
                    pluginApis[pluginId] = apis = new List<object>();

                // Re-exporting a new instance of the same concrete type replaces the old one.
                apis.RemoveAll(a => a.GetType() == api.GetType());
                apis.Add(api);
            }
        }

        /// <summary>Fetches an API object exported by the given plugin id that is assignable to <typeparamref name="T"/> (see <see cref="IOsuCcPluginHost.GetApi{T}"/>).</summary>
        internal static T? GetPluginApi<T>(string pluginId) where T : class
        {
            lock (lockObject)
            {
                if (pluginApis.TryGetValue(pluginId, out var apis))
                {
                    foreach (object api in apis)
                    {
                        if (api is T typed)
                            return typed;
                    }
                }

                return null;
            }
        }

        private static IEnumerable<Type> getLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                TimingLog.Error($"PluginManager: {assembly.FullName} failed to load {e.Types.Count(t => t == null)} type(s); loader exceptions:");

                foreach (var loaderException in e.LoaderExceptions.Distinct())
                    TimingLog.Error($"  {loaderException}");

                return e.Types.Where(t => t != null)!;
            }
        }
    }
}
