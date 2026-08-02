using osucc.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace osucc.Plugin
{
    /// <summary>
    /// Resolves where osu-cc keeps its per-game data (plugins, logs, <c>game.ini</c>) without
    /// OS-specific hardcoding. Mirrors how osu.Framework picks the game's user storage:
    /// <c>%AppData%</c> on Windows, <c>~/.local/share</c> elsewhere (plus legacy macOS path),
    /// and the exe directory for portable installs (a <c>framework.ini</c> next to it). The
    /// <c>osu-cc</c> folder lives under the game's data folder (named <c>osu</c> /
    /// <c>osu-development</c> / … depending on the build).
    /// </summary>
    internal static class PluginDirectories
    {
        public const string PluginsDirectoryName = "plugins";
        public const string OsuCcDirectoryName = "osu-cc";

        /// <summary>Path of the folder scanned for plugin archives / plugin folders.</summary>
        public static string ResolvePluginsDirectory()
            => Path.Combine(ResolveOsuCcDirectory(), PluginsDirectoryName);

        /// <summary>Path of the folder holding per-session osu-cc logs.</summary>
        public static string ResolveLogsDirectory()
            => Path.Combine(ResolveOsuCcDirectory(), "logs");

        /// <summary>
        /// The osu-cc data root: where the client keeps <c>plugins</c>, <c>game.ini</c> and the
        /// per-session logs. Shares the game's data folder (named <c>osu</c> /
        /// <c>osu-development</c> / … depending on the build).
        /// </summary>
        public static string ResolveOsuCcDirectory()
        {
            // Portable install: framework.ini sits next to the hook DLL in the startup dir;
            // osu stores everything there (DesktopGameHost.GetDefaultGameStorage).
            string hookDirectory = Path.GetDirectoryName(typeof(PluginManager).Assembly.Location) ?? string.Empty;

            if (hookDirectory.Length > 0 && File.Exists(Path.Combine(hookDirectory, "framework.ini")))
                return Path.Combine(hookDirectory, OsuCcDirectoryName);

            // Prefer an existing data folder that already has an osu-cc directory (created on
            // earlier runs). This handles "osu", "osu-development", "osu-development-2", …
            // without knowing the build's game name.
            foreach (string storagePath in userStoragePaths())
            {
                if (!Directory.Exists(storagePath))
                    continue;

                foreach (string gameDirectory in Directory.GetDirectories(storagePath))
                {
                    string osuCc = Path.Combine(gameDirectory, OsuCcDirectoryName);

                    if (Directory.Exists(osuCc))
                        return osuCc;
                }
            }

            // Fallback: standard game data folder + osu-cc (created on demand).
            foreach (string storagePath in userStoragePaths())
            {
                string path = Path.Combine(storagePath, "osu", OsuCcDirectoryName);

                try
                {
                    Directory.CreateDirectory(path);
                    return path;
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"PluginDirectories: cannot create {path}: {ex}");
                }
            }

            return Path.Combine(userStoragePaths().First(), "osu", OsuCcDirectoryName);
        }

        private static IEnumerable<string> userStoragePaths()
        {
            // WindowsGameHost.UserStoragePaths -> Roaming AppData.
            if (OperatingSystem.IsWindows())
                return new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create),
                };

            var paths = new List<string>
            {
                // Base GameHost.UserStoragePaths -> LocalApplicationData (~/.local/share on Linux/macOS).
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
            };

            // MacOSGameHost additionally yields the legacy ~/.local/share path.
            if (OperatingSystem.IsMacOS())
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                if (home.Length > 0)
                    paths.Add(Path.Combine(home, ".local", "share"));
            }

            return paths;
        }
    }
}
