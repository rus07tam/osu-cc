using osucc.Core;

namespace osucc.Plugin
{
    /// <summary>
    /// Persists each plugin's enabled state (<c>true</c> unless explicitly disabled), a
    /// per-plugin priority override (empty when the attribute's priority is used) and pending
    /// deletions in a simple <c>key = value</c> ini next to the plugins folder. Enabled/priority
    /// changes apply on the next launch; disabled plugins stay listed so they can be re-enabled,
    /// and pending-delete folders are removed on the next launch before anything is loaded.
    /// </summary>
    public static class PluginStateStore
    {
        private const string priorityPrefix = "priority.";
        private const string deletePrefix = "delete.";
        private const string versionPrefix = "version.";
        private const string schemaPrefix = "schema.";

        private static readonly Dictionary<string, bool> states = new();
        private static readonly Dictionary<string, int> priorities = new();
        private static readonly HashSet<string> deleted = new();
        private static readonly Dictionary<string, string> versions = new();
        private static readonly Dictionary<string, int> schemas = new();

        private static string? path;

        /// <summary>Sets the backing file path; must be called before any other member.</summary>
        public static void Initialise(string statesPath)
        {
            path = statesPath;
        }

        /// <summary>Reads the backing file into memory. No-op if the file is missing or unreadable.</summary>
        public static void Load()
        {
            states.Clear();
            priorities.Clear();
            deleted.Clear();
            versions.Clear();
            schemas.Clear();

            try
            {
                if (path == null || !File.Exists(path))
                    return;

                foreach (string line in File.ReadAllLines(path))
                {
                    int equalsIndex = line.IndexOf('=');

                    if (line.Length == 0 || line[0] == '#' || equalsIndex < 0)
                        continue;

                    string key = line[..equalsIndex].Trim();
                    string value = line[(equalsIndex + 1)..].Trim();

                    if (key.StartsWith(priorityPrefix, StringComparison.Ordinal))
                    {
                        string id = key[priorityPrefix.Length..].Trim();

                        if (id.Length > 0 && int.TryParse(value, out int priority))
                            priorities[id] = priority;

                        continue;
                    }

                    if (key.StartsWith(deletePrefix, StringComparison.Ordinal))
                    {
                        string id = key[deletePrefix.Length..].Trim();

                        if (id.Length > 0 && bool.TryParse(value, out bool isDeleted) && isDeleted)
                            deleted.Add(id);

                        continue;
                    }

                    if (key.StartsWith(versionPrefix, StringComparison.Ordinal))
                    {
                        string id = key[versionPrefix.Length..].Trim();

                        if (id.Length > 0 && value.Length > 0)
                            versions[id] = value;

                        continue;
                    }

                    if (key.StartsWith(schemaPrefix, StringComparison.Ordinal))
                    {
                        string id = key[schemaPrefix.Length..].Trim();

                        if (id.Length > 0 && int.TryParse(value, out int schema))
                            schemas[id] = schema;

                        continue;
                    }

                    if (key.Length > 0 && bool.TryParse(value, out bool enabled))
                        states[key] = enabled;
                }
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PluginStateStore: failed to read states: {ex}");
            }
        }

        /// <summary>Whether the given plugin id is enabled (defaults to enabled when unset).</summary>
        public static bool IsEnabled(string id) => states.TryGetValue(id, out bool enabled) ? enabled : true;

        /// <summary>Sets and persists the enabled state for a plugin id.</summary>
        public static void SetEnabled(string id, bool enabled)
        {
            states[id] = enabled;
            save();
        }

        /// <summary>The persisted priority override for a plugin id, or <c>null</c> to use the attribute's priority.</summary>
        public static int? GetPriority(string id) => priorities.TryGetValue(id, out int priority) ? priority : null;

        /// <summary>Sets and persists the priority override for a plugin id.</summary>
        public static void SetPriority(string id, int priority)
        {
            priorities[id] = priority;
            save();
        }

        /// <summary>Sets and persists priority overrides for several plugin ids in a single write.</summary>
        public static void SetPriorities(IEnumerable<KeyValuePair<string, int>> overrides)
        {
            foreach (var pair in overrides)
                priorities[pair.Key] = pair.Value;

            save();
        }

        /// <summary>Whether deletion of the given plugin id is pending (folder removed on the next launch).</summary>
        public static bool IsDeleted(string id) => deleted.Contains(id);

        /// <summary>Marks the given plugin id for deletion on the next launch.</summary>
        public static void MarkDeleted(string id)
        {
            deleted.Add(id);
            save();
        }

        /// <summary>Cancels a pending deletion for the given plugin id (see <see cref="MarkDeleted"/>).</summary>
        public static void UnmarkDeleted(string id)
        {
            deleted.Remove(id);
            save();
        }

        /// <summary>Snapshot of all pending-delete plugin ids.</summary>
        public static IEnumerable<string> DeletedIds => deleted.ToArray();

        /// <summary>The plugin version recorded on the last launch, or <c>null</c> if never tracked (fresh install).</summary>
        public static string? GetVersion(string id) => versions.TryGetValue(id, out string? version) ? version : null;

        /// <summary>Sets and persists the last-seen plugin version.</summary>
        public static void SetVersion(string id, string version)
        {
            versions[id] = version;
            save();
        }

        /// <summary>The last data schema version applied for a plugin id, or <c>null</c> if never tracked.</summary>
        public static int? GetSchemaVersion(string id) => schemas.TryGetValue(id, out int schema) ? schema : null;

        /// <summary>Sets and persists the applied data schema version for a plugin id.</summary>
        public static void SetSchemaVersion(string id, int schema)
        {
            schemas[id] = schema;
            save();
        }

        /// <summary>
        /// Forgets the recorded version and data schema for a plugin id so the plugin's data
        /// migrations re-run on the next launch (used by "clear plugin data"). Enabled state,
        /// priority and pending deletion are kept.
        /// </summary>
        public static void ClearData(string id)
        {
            versions.Remove(id);
            schemas.Remove(id);
            save();
        }

        /// <summary>Forgets everything persisted for a plugin id (enabled state, priority, pending deletion, version, schema).</summary>
        public static void Remove(string id)
        {
            states.Remove(id);
            priorities.Remove(id);
            deleted.Remove(id);
            versions.Remove(id);
            schemas.Remove(id);
            save();
        }

        private static void save()
        {
            try
            {
                if (path == null)
                    return;

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                var lines = new List<string>(states.Count + priorities.Count + deleted.Count + versions.Count + schemas.Count);

                foreach (var pair in states)
                    lines.Add($"{pair.Key} = {pair.Value}");

                foreach (var pair in priorities)
                    lines.Add($"{priorityPrefix}{pair.Key} = {pair.Value}");

                foreach (string id in deleted)
                    lines.Add($"{deletePrefix}{id} = true");

                foreach (var pair in versions)
                    lines.Add($"{versionPrefix}{pair.Key} = {pair.Value}");

                foreach (var pair in schemas)
                    lines.Add($"{schemaPrefix}{pair.Key} = {pair.Value}");

                File.WriteAllLines(path, lines);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PluginStateStore: failed to write states: {ex}");
            }
        }
    }
}
