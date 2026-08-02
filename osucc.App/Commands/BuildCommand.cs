using DotMake.CommandLine;

namespace osucc.App.Commands;

/// <summary>Builds the hook and the plugins.</summary>
[CliCommand(Description = "Build the hook and the plugins.")]
public class BuildCommand
{
    public RootCliCommand Root { get; set; } = null!;

    public int Run() => BuildRunner.Build(Root.ResolvePaths().RepoRoot, Root.Config);
}
