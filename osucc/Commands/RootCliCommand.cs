using DotMake.CommandLine;
using osucc.Common;

namespace osucc.App.Commands;

/// <summary>
/// osu-cc launcher: launches osu! with the startup hook deployed in the osu-cc data root. Bare
/// <c>osucc</c> shows help (no default action); <c>osucc start</c>/<c>osucc run</c> apply any
/// staged update and launch, <c>osucc status</c> inspects the installation. The launcher never
/// builds and never touches the install dir; the hook is installed and updated by the in-game
/// updater plugin. The options are recursive, so they apply to every subcommand.
/// </summary>
[CliCommand(
    Description = "osu-cc launcher: launch osu! with the startup hook from the osu-cc data root (never builds, never touches the install dir).",
    Children = new[]
    {
        typeof(RunCommand),
        typeof(StartCommand),
        typeof(StatusCommand),
    })]
public class RootCliCommand
{
    /// <summary>Path to the osu! install directory.</summary>
    [CliOption(Description = "Path to the osu! install directory.", Required = false, Recursive = true)]
    public string? OsuDir { get; set; }

    /// <summary>Print extra diagnostics while working.</summary>
    [CliOption(Description = "Print extra diagnostics while working.", Alias = "-v", Recursive = true)]
    public bool Verbose { get; set; }

    public ResolvedPaths ResolvePaths()
    {
        string osuDirectory = OsuCcPaths.ResolveOsuDirectory(OsuDir);
        string osuCcDirectory = OsuCcDataRootResolver.Resolve(AppContext.BaseDirectory);

        return new ResolvedPaths(
            osuDirectory,
            osuCcDirectory,
            OsuCcDataRootResolver.ResolveHookDirectory(osuCcDirectory),
            OsuCcDataRootResolver.ResolvePluginsDirectory(osuCcDirectory),
            OsuCcDataRootResolver.ResolveStagingDirectory(osuCcDirectory));
    }
}
