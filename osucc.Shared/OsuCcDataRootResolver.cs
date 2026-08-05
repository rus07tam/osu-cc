using osucc.Common;

namespace osucc.Common;

/// <summary>
/// Resolves where osu-cc keeps its per-game data (the <c>osu-cc</c> folder with <c>hook</c>,
/// <c>plugins</c> and per-session logs) without OS-specific hardcoding. Mirrors how
/// osu.Framework picks the game's user storage: <c>%AppData%</c> on Windows,
/// <c>~/.local/share</c> elsewhere (plus legacy macOS path), and the exe directory for portable
/// installs (a <c>framework.ini</c> next to the executable). The <c>osu-cc</c> folder lives under
/// the game's data folder (named <c>osu</c> / <c>osu-development</c> / … depending on the build).
/// <para>
/// The single implementation shared by the launcher (portable base = the launcher's directory)
/// and the hook (portable base = the hook DLL's directory), so both always agree on the root.
/// </para>
/// </summary>
public static class OsuCcDataRootResolver
{
    /// <summary>
    /// The osu-cc data root for the given portable base directory. When the base directory holds
    /// a <c>framework.ini</c> (portable install) the root sits inside it; otherwise an existing
    /// <c>osu-cc</c> folder under any game data folder wins, and a fresh one under the standard
    /// <c>osu</c> data folder is created on demand.
    /// </summary>
    public static string Resolve(string portableBaseDirectory)
    {
        // Portable install: framework.ini next to the executable means osu keeps everything there.
        if (!string.IsNullOrEmpty(portableBaseDirectory) && File.Exists(Path.Combine(portableBaseDirectory, "framework.ini")))
            return Path.Combine(portableBaseDirectory, OsuCcLayout.OsuCcDirectoryName);

        // Prefer an existing data folder that already has an osu-cc directory (created on
        // earlier runs). This handles "osu", "osu-development", "osu-development-2", …
        // without knowing the build's game name.
        foreach (string storagePath in UserStoragePaths())
        {
            if (!Directory.Exists(storagePath))
                continue;

            foreach (string gameDirectory in Directory.GetDirectories(storagePath))
            {
                string osuCc = Path.Combine(gameDirectory, OsuCcLayout.OsuCcDirectoryName);

                if (Directory.Exists(osuCc))
                    return osuCc;
            }
        }

        // Fallback: standard game data folder + osu-cc (created on demand).
        foreach (string storagePath in UserStoragePaths())
        {
            string path = Path.Combine(storagePath, "osu", OsuCcLayout.OsuCcDirectoryName);

            try
            {
                Directory.CreateDirectory(path);
                return path;
            }
            catch (Exception ex)
            {
                OsuCcTimingLog.ReportError($"cannot create {path}: {ex}");
            }
        }

        return Path.Combine(UserStoragePaths().First(), "osu", OsuCcLayout.OsuCcDirectoryName);
    }

    /// <summary>Path of the <c>hook</c> folder under the given data root.</summary>
    public static string ResolveHookDirectory(string osuCcDirectory)
        => Path.Combine(osuCcDirectory, OsuCcLayout.HookDirectoryName);

    /// <summary>Path of the <c>plugins</c> folder under the given data root.</summary>
    public static string ResolvePluginsDirectory(string osuCcDirectory)
        => Path.Combine(osuCcDirectory, OsuCcLayout.PluginsDirectoryName);

    /// <summary>Path of the <c>staging</c> folder under the given data root.</summary>
    public static string ResolveStagingDirectory(string osuCcDirectory)
        => Path.Combine(osuCcDirectory, OsuCcLayout.StagingDirectoryName);

    /// <summary>
    /// The candidate user storage roots: <c>%AppData%</c> (Roaming) on Windows, LocalApplicationData
    /// (<c>~/.local/share</c>) elsewhere, plus the legacy <c>~/.local/share</c> path macOS yields.
    /// </summary>
    public static IEnumerable<string> UserStoragePaths()
    {
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
