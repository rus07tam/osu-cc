using DotMake.CommandLine;

namespace osucc.App.Commands;

/// <summary>Applies any staged update, then launches osu! with the deployed hook (no build; works without a local checkout).</summary>
[CliCommand(Description = "Apply any staged update, then launch osu! with the deployed hook (no build).")]
public class RunCommand
{
    public RootCliCommand Root { get; set; } = null!;

    public int Run() => LauncherPipeline.Run(Root.ResolvePaths());
}
