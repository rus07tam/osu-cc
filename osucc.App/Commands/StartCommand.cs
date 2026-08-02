using DotMake.CommandLine;

namespace osucc.App.Commands;

/// <summary>Builds the hook and the plugins, deploys them, then launches osu! with the hook.</summary>
[CliCommand(
    Description = "Build, deploy the hook and plugins, then launch osu! with it.",
    ShortFormAutoGenerate = CliNameAutoGenerate.None)]
public class StartCommand
{
    public RootCliCommand Root { get; set; } = null!;

    public int Run() => Pipeline.Run(Root.ResolvePaths(), Root.NoBuild);
}
