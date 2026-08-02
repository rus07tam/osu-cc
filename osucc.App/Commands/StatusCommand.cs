using DotMake.CommandLine;

namespace osucc.App.Commands;

/// <summary>Shows resolved paths and deployment state.</summary>
[CliCommand(Description = "Show resolved paths and deployment state.")]
public class StatusCommand
{
    public RootCliCommand Root { get; set; } = null!;

    private static readonly string[] legacyBlobNames = { "osucc.dll", "0Harmony.dll", "SharpCompress.dll" };

    public int Run()
    {
        var paths = Root.ResolvePaths();

        Console.WriteLine($"Repo root     : {paths.RepoRoot ?? "(not found - pass --repo for build/deploy)"}");
        Console.WriteLine($"osu install   : {paths.OsuDirectory} ({(File.Exists(OsuCcPaths.ResolveExecutable(paths.OsuDirectory)) ? "osu! found" : "no osu! executable")})");
        Console.WriteLine($"osu-cc data   : {Path.GetDirectoryName(paths.HookDirectory)}");
        Console.WriteLine($"Hook dir      : {paths.HookDirectory} ({(File.Exists(OsuCcPaths.ResolveHookDll(paths.HookDirectory)) ? "deployed" : "empty")})");
        Console.WriteLine($"Plugins dir   : {paths.PluginsDirectory}");

        foreach (string dll in legacyBlobNames)
        {
            string legacy = Path.Combine(paths.OsuDirectory, dll);
            if (File.Exists(legacy))
                Console.WriteLine($"Legacy blob   : {legacy} (present - run 'osucc clean')");
        }

        return 0;
    }
}
