using DotMake.CommandLine;

namespace osucc.App.Commands;

/// <summary>Launches osu! with the already-deployed hook (no build or deploy; works without a local checkout).</summary>
[CliCommand(Description = "Launch osu! with the already-deployed hook (no build or deploy).")]
public class RunCommand
{
    public RootCliCommand Root { get; set; } = null!;

    public int Run()
    {
        var paths = Root.ResolvePaths();
        return GameLauncher.Launch(paths.OsuDirectory, OsuCcPaths.ResolveHookDll(paths.HookDirectory));
    }
}
