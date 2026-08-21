using System;
using System.IO;
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
        private static Func<string>? logDirectoryProvider;

        public static Func<string>? LogDirectoryProvider
        {
            get => logDirectoryProvider;
            set
            {
                lock (lockObject)
                {
                    logDirectoryProvider = value;
                    logDirectory = null;
                }
            }
        }

        /// <summary>Path of this session's log file.</summary>
        public static string LogPath
        {
            get
            {
                string directory = resolveLogDirectory();
                return Path.Combine(directory, $"{sessionStartupTimestamp}.osu-cc.log");
            }
        }

        public static void Debug(string message) => write(LogLevel.Debug, message);

        public static void Info(string message) => write(LogLevel.Info, message);

        public static void Warn(string message) => write(LogLevel.Warn, message);

        public static void Error(string message) => write(LogLevel.Error, message);

        private static void write(LogLevel level, string message)
        {
            try
            {
                if (level == LogLevel.Error)
                    Console.Error.WriteLine($"[osu-cc] [{DateTime.Now:HH:mm:ss.fff}] [ERROR] {message}");

                lock (lockObject)
                {
                    string path = LogPath;

                    File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss.fff}] [{level.ToString().ToUpperInvariant()}] {message}{Environment.NewLine}", Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[osu-cc] Logging failed: {ex}");
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
                if (LogDirectoryProvider != null)
                {
                    logDirectory = LogDirectoryProvider();
                }
                else
                {
                    logDirectory = Path.Combine(Path.GetTempPath(), "osu-cc");
                }

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
