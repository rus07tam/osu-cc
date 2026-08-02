using DotMake.CommandLine;

namespace osucc.App.Commands;

/// <summary>
/// osu-cc launcher: build, deploy and run osu! with the startup hook. Bare <c>osucc</c> shows
/// help (no default action); <c>osucc start</c> performs build + deploy + run and
/// <c>osucc run</c> only launches the already-deployed hook. The options are recursive,
/// so they apply to every subcommand.
/// </summary>
[CliCommand(
    Description = "osu-cc launcher: build, deploy and run osu! with the startup hook (never touches the install dir).",
    Children = new[]
    {
        typeof(BuildCommand),
        typeof(DeployCommand),
        typeof(RunCommand),
        typeof(StartCommand),
        typeof(UpdateCommand),
        typeof(CleanCommand),
        typeof(StatusCommand),
    })]
public class RootCliCommand
{
    /// <summary>Path to the osu! install directory.</summary>
    [CliOption(Description = "Path to the osu! install directory.", Required = false, Recursive = true)]
    public string? OsuDir { get; set; }

    /// <summary>Path to the osu-cc repository root.</summary>
    [CliOption(Description = "Path to the osu-cc repository root.", Required = false, Recursive = true)]
    public string? Repo { get; set; }

    /// <summary>Build configuration (Debug or Release).</summary>
    [CliOption(Description = "Build configuration (Debug or Release).", Alias = "-c", Recursive = true)]
    public string Config { get; set; } = "Debug";

    /// <summary>Skip building before running.</summary>
    [CliOption(Description = "Skip building before running.", Recursive = true)]
    public bool NoBuild { get; set; }

    public ResolvedPaths ResolvePaths()
        => new(
            Config,
            OsuCcPaths.ResolveRepoRoot(Repo),
            OsuCcPaths.ResolveOsuDirectory(OsuDir),
            OsuCcPaths.ResolveHookDirectory(),
            OsuCcPaths.ResolvePluginsDirectory());
}
