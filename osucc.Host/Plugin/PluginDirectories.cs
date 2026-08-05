using osucc.Common;
using osucc.Core;

namespace osucc.Plugin
{
    /// <summary>
    /// Resolves where osu-cc keeps its per-game data (plugins, logs, <c>game.ini</c>) without
    /// OS-specific hardcoding. Thin wrapper over the shared
    /// <see cref="OsuCcDataRootResolver"/> — the same resolver the osucc launcher uses, so the
    /// updater plugin stages where the launcher applies. Mirrors how osu.Framework picks the
    /// game's user storage: <c>%AppData%</c> on Windows, <c>~/.local/share</c> elsewhere (plus
    /// legacy macOS path), and the exe directory for portable installs (a <c>framework.ini</c>
    /// next to it). The <c>osu-cc</c> folder lives under the game's data folder (named
    /// <c>osu</c> / <c>osu-development</c> / … depending on the build).
    /// </summary>
    internal static class PluginDirectories
    {
        /// <summary>Path of the folder scanned for plugin archives / plugin folders.</summary>
        public static string ResolvePluginsDirectory()
            => Path.Combine(ResolveOsuCcDirectory(), OsuCcLayout.PluginsDirectoryName);

        /// <summary>Path of the folder holding per-session osu-cc logs.</summary>
        public static string ResolveLogsDirectory()
            => Path.Combine(ResolveOsuCcDirectory(), "logs");

        /// <summary>
        /// The osu-cc data root: where the client keeps <c>plugins</c>, <c>game.ini</c> and the
        /// per-session logs. Shares the game's data folder (named <c>osu</c> /
        /// <c>osu-development</c> / … depending on the build). Resolved through the shared
        /// resolver; the portable base is the hook DLL's directory.
        /// </summary>
        public static string ResolveOsuCcDirectory()
        {
            string hookDirectory = Path.GetDirectoryName(typeof(PluginManager).Assembly.Location) ?? string.Empty;
            return OsuCcDataRootResolver.Resolve(hookDirectory);
        }
    }
}
