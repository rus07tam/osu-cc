using DotMake.CommandLine;

namespace osucc.App.Commands;

/// <summary>Deploys the hook and the plugin archives into the osu-cc data root.</summary>
[CliCommand(Description = "Deploy the hook and the plugin archives into the osu-cc data root.")]
public class DeployCommand
{
    public RootCliCommand Root { get; set; } = null!;

    public int Run()
    {
        var paths = Root.ResolvePaths();

        if (paths.RepoRoot == null)
        {
            Console.Error.WriteLine("ERROR: cannot locate repo root (osucc.sln). Pass --repo.");
            return 1;
        }

        bool ok = HookDeployer.Deploy(paths.RepoRoot, paths.Config, paths.HookDirectory);
        PluginDeployer.Deploy(paths.RepoRoot, paths.Config, paths.PluginsDirectory);
        return ok ? 0 : 1;
    }
}
