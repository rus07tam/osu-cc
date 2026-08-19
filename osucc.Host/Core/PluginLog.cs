using osucc.Plugin;
using System.Text;

namespace osucc.Core
{
    /// <summary>
    /// Per-plugin append-only log, one <c>{sessionTimestamp}.{pluginId}.osu-cc.log</c> per plugin
    /// per launch, in the osu-cc data root's <c>logs</c> folder. Keeps a plugin's own diagnostics
    /// isolated from the shared session log (<see cref="TimingLog"/>). Line format matches
    /// <see cref="TimingLog"/>: <c>[HH:mm:ss.fff] [LEVEL] message</c>. Plugins are only identified
    /// by their stable id, so the plugin name must be supplied where useful.
    /// </summary>
    public static class PluginLog
    {
        private static readonly object lockObject = new();

        private static readonly long sessionStartupTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Appends a line at the given level to the given plugin's log file. Never throws.</summary>
        public static void Write(string pluginId, LogLevel level, string message)
        {
            try
            {
                string path = resolvePath(pluginId);
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level.ToString().ToUpperInvariant()}] {message}{Environment.NewLine}";

                lock (lockObject)
                    File.AppendAllText(path, line, Encoding.UTF8);
            }
            catch
            {
                // Never let logging break the hook or the plugin.
            }
        }

        private static string resolvePath(string pluginId)
        {
            string directory = PluginDirectories.ResolveLogsDirectory();

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return Path.Combine(directory, $"{sessionStartupTimestamp}.{sanitize(pluginId)}.osu-cc.log");
        }

        /// <summary>Plugin ids come from user-provided packages, so strip path separators before using them in a filename.</summary>
        private static string sanitize(string pluginId)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            int length = pluginId.Length;
            var builder = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                char c = pluginId[i];
                builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }

            return builder.Length == 0 ? "plugin" : builder.ToString();
        }
    }
}
