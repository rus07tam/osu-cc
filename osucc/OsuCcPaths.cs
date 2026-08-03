namespace osucc.App;

/// <summary>
/// Resolves the paths osucc works with: the repo root (walk-up from the app base directory until
/// <c>osucc.sln</c>), the osu! install dir, and the osu-cc data root (same logic as
/// <c>osucc.Plugin.PluginDirectories</c>, so the hook finds what we deploy). The hook DLLs
/// live in <c>&lt;osu-cc&gt;/hook</c>, never inside the osu install dir. <c>AppContext.BaseDirectory</c>
/// is used instead of <c>Assembly.Location</c> because the latter is empty for single-file publishes.
/// </summary>
internal static class OsuCcPaths
{
    public const string OsuCcDirectoryName = "osu-cc";
    public const string HookDirectoryName = "hook";
    public const string PluginsDirectoryName = "plugins";

    public static string? ResolveRepoRoot(string? overridePath)
    {
        if (!string.IsNullOrEmpty(overridePath))
            return Path.GetFullPath(overridePath);

        string? directory = AppContext.BaseDirectory;

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory, "osucc.sln")))
                return directory;

            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }

    public static string ResolveOsuDirectory(string? overridePath)
    {
        if (!string.IsNullOrEmpty(overridePath))
            return Path.GetFullPath(overridePath);

        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osulazer", "current");

        // osu!lazer on macOS keeps the app under the user's Applications folder.
        if (OperatingSystem.IsMacOS())
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Applications", "osu!.app");
        }

        return resolveLinuxOsuDirectory()
               ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "osu", "current");
    }

    /// <summary>
    /// Locates the Linux osu! install: the fully-resolved <c>osu!</c> found on PATH (nixpkgs, AUR,
    /// symlinked installs), then a few common install directories. Returns <c>null</c> when nothing
    /// is found so the caller falls back to the historical default.
    /// </summary>
    private static string? resolveLinuxOsuDirectory()
    {
        string? onPath = resolveOnPath("osu!");

        if (onPath != null)
        {
            string executable = resolveSymlinks(onPath);
            string? directory = Path.GetDirectoryName(executable);

            if (directory != null && File.Exists(Path.Combine(directory, "osu!")))
                return directory;
        }

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        foreach (string candidate in new[]
        {
            Path.Combine(home, ".local", "share", "osu", "current"),
            Path.Combine(home, ".local", "share", "osu-lazer"),
            Path.Combine(home, ".local", "share", "osu-lazer-bin"),
            "/opt/osu-lazer",
            "/opt/osu-lazer-bin",
        })
        {
            if (File.Exists(Path.Combine(candidate, "osu!")))
                return candidate;
        }

        return null;
    }

    private static string? resolveOnPath(string fileName)
    {
        string? pathValue = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(pathValue))
            return null;

        foreach (string directory in pathValue.Split(Path.PathSeparator))
        {
            if (directory.Length == 0)
                continue;

            string candidate = Path.Combine(directory, fileName);

            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>Follows symlinks to the real file, returning the original path on failure (best-effort detection).</summary>
    private static string resolveSymlinks(string path)
    {
        try
        {
            return new FileInfo(path).ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? path;
        }
        catch (Exception)
        {
            return path;
        }
    }

    public static string ResolveExecutable(string osuDirectory)
    {
        string name = OperatingSystem.IsWindows() ? "osu!.exe" : "osu!";
        return Path.Combine(osuDirectory, name);
    }

    /// <summary>Path of the osu-cc data root (same as <c>PluginDirectories.ResolveOsuCcDirectory()</c>).</summary>
    public static string ResolveOsuCcDirectory()
    {
        string launcherDirectory = AppContext.BaseDirectory;

        // Portable install: a framework.ini next to the launcher means osu keeps everything there.
        if (launcherDirectory.Length > 0 && File.Exists(Path.Combine(launcherDirectory, "framework.ini")))
            return Path.Combine(launcherDirectory, OsuCcDirectoryName);

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
                Console.Error.WriteLine($"osucc: cannot create {path}: {ex.Message}");
            }
        }

        return Path.Combine(userStoragePaths().First(), "osu", OsuCcDirectoryName);
    }

    public static string ResolveHookDirectory()
        => Path.Combine(ResolveOsuCcDirectory(), HookDirectoryName);

    public static string ResolvePluginsDirectory()
        => Path.Combine(ResolveOsuCcDirectory(), PluginsDirectoryName);

    /// <summary>Path of the hook's build output for a given configuration.</summary>
    public static string ResolveHookOutput(string repoRoot, string config)
        => Path.Combine(repoRoot, "osucc.Host", "bin", config, "net8.0");

    /// <summary>Path of the osucc.dll startup hook that gets loaded by the game.</summary>
    public static string ResolveHookDll(string hookDirectory)
        => Path.Combine(hookDirectory, "osucc.dll");

    private static IEnumerable<string> userStoragePaths()
    {
        if (OperatingSystem.IsWindows())
            return new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create),
            };

        var paths = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
        };

        if (OperatingSystem.IsMacOS())
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (home.Length > 0)
                paths.Add(Path.Combine(home, ".local", "share"));
        }

        return paths;
    }
}
