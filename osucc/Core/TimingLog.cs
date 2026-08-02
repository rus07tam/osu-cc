using osucc.Plugin;
using System.Text;

namespace osucc.Core
{
    /// <summary>
    /// Thread-safe append-only per-session log, mirroring osu.Framework's layout: one
    /// <c>{unixTimestamp}.osu-cc.log</c> per launch, in the osu-cc data root's <c>logs</c>
    /// folder. Used to prove patch-install timing and trace client API events. Sessions older
    /// than 7 days are pruned on the first write of a new one.
    /// </summary>
    public static class TimingLog
    {
        private static readonly object lockObject = new();

        // Same scheme as osu.Framework's Logger: a stable per-session prefix so all of a
        // session's events land in one file.
        private static readonly long sessionStartupTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Resolved lazily and guarded: resolving asks PluginDirectories, which logs its own
        // failures via TimingLog.Error — never re-enter that from within a resolve attempt.
        private static string? logDirectory;
        private static bool resolvingLogDirectory;

        /// <summary>Path of this session's log file.</summary>
        public static string LogPath
        {
            get
            {
                string directory = resolveLogDirectory();
                return Path.Combine(directory, $"{sessionStartupTimestamp}.osu-cc.log");
            }
        }

        public static void Info(string message) => write("INFO", message);

        public static void Error(string message) => write("ERROR", message);

        private static void write(string level, string message)
        {
            try
            {
                lock (lockObject)
                {
                    string path = LogPath;

                    File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}", Encoding.UTF8);
                }
            }
            catch
            {
                // Never let logging break the hook.
            }
        }

        private static string resolveLogDirectory()
        {
            // A concurrent or re-entrant resolve returns the default without recursing; the
            // first successful call wins and is cached for the rest of the session.
            if (logDirectory != null)
                return logDirectory;

            if (resolvingLogDirectory)
                return Path.Combine(Path.GetTempPath(), "osu-cc");

            resolvingLogDirectory = true;

            try
            {
                logDirectory = PluginDirectories.ResolveLogsDirectory();

                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);

                pruneOldLogs(logDirectory);
                return logDirectory;
            }
            catch
            {
                // Fall back to a writable location rather than letting logging fail.
                return logDirectory = Path.Combine(Path.GetTempPath(), "osu-cc");
            }
            finally
            {
                resolvingLogDirectory = false;
            }
        }

        /// <summary>Deletes osu-cc session logs older than 7 days, like osu.Framework's LogCycle.</summary>
        private static void pruneOldLogs(string directory)
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-7);

            foreach (string file in Directory.GetFiles(directory, "*.osu-cc.log"))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoff)
                        File.Delete(file);
                }
                catch
                {
                    // Locked or just-created; leave it for the next session.
                }
            }
        }
    }
}
