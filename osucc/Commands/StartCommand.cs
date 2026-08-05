using DotMake.CommandLine;

namespace osucc.App.Commands;

/// <summary>
/// Launches osu! with the hook. Kept as a sibling of <c>run</c> for muscle memory from the old
/// build+deploy+run flow: since the launcher no longer builds, both apply any staged update and
/// launch identically.
/// </summary>
[CliCommand(
    Description = "Apply any staged update, then launch osu! with the deployed hook (same as run).",
    ShortFormAutoGenerate = CliNameAutoGenerate.None)]
public class StartCommand
{
    public RootCliCommand Root { get; set; } = null!;

    public int Run() => LauncherPipeline.Run(Root.ResolvePaths());
}
