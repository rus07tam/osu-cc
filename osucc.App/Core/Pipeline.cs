namespace osucc.App;

/// <summary>The build → deploy → launch flow behind <c>osucc start</c>.</summary>
internal static class Pipeline
{
    public static int Run(ResolvedPaths paths, bool noBuild)
    {
        if (!noBuild)
        {
            int buildCode = BuildRunner.Build(paths.RepoRoot, paths.Config);

            if (buildCode != 0)
                return buildCode;
        }

        // Deploying requires the repo's build output; with --no-build and no local checkout the
        // hook must already be deployed in the data root (from an earlier build/deploy).
        if (paths.RepoRoot != null && !HookDeployer.Deploy(paths.RepoRoot, paths.Config, paths.HookDirectory))
            return 1;

        // start = build + deploy + run: fresh plugin archives are (re)installed like `osucc deploy`,
        // so a deleted plugin comes back here (unlike `osucc run`, which never touches the archives).
        if (paths.RepoRoot != null)
            PluginDeployer.Deploy(paths.RepoRoot, paths.Config, paths.PluginsDirectory);

        return GameLauncher.Launch(paths.OsuDirectory, OsuCcPaths.ResolveHookDll(paths.HookDirectory));
    }
}
