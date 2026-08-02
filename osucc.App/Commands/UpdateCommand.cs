using DotMake.CommandLine;
using osucc.App.Updater;

namespace osucc.App.Commands;

/// <summary>
/// Pulls the latest hook and plugin archives from the public feeds (nuget.org + GitHub
/// releases), so the game can be kept current without a local build. Add <c>--launcher</c>
/// to also self-update the launcher.
/// </summary>
[CliCommand(
    Description = "Update the hook and plugins from NuGet/GitHub releases (add --launcher to also update osucc itself).",
    ShortFormAutoGenerate = CliNameAutoGenerate.None)]
public class UpdateCommand
{
    public RootCliCommand Root { get; set; } = null!;

    /// <summary>Also update the launcher itself (dotnet tool or standalone binary).</summary>
    [CliOption(Description = "Also update the osucc launcher itself.")]
    public bool Launcher { get; set; }

    public async Task<int> RunAsync()
    {
        var paths = Root.ResolvePaths();

        using var http = new HttpClient();
        int exitCode = 0;

        if (Launcher)
            exitCode = Math.Max(exitCode, await LauncherUpdater.UpdateAsync(http));

        exitCode = Math.Max(exitCode, await HookUpdater.UpdateAsync(http, paths.HookDirectory));
        exitCode = Math.Max(exitCode, await PluginUpdater.UpdateAsync(http, paths.PluginsDirectory));

        return exitCode;
    }
}
