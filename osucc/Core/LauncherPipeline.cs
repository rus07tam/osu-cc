using osucc.Common;

namespace osucc.App;

/// <summary>The shared run routine behind <c>osucc run</c> and <c>osucc start</c>: apply any staged
/// update, then launch osu! with the hook. Returns the osu! exit code, or a non-zero code when the
/// hook is missing.</summary>
internal static class LauncherPipeline
{
    public static int Run(ResolvedPaths paths)
    {
        string hookDll = Path.Combine(paths.HookDirectory, OsuCcLayout.HookDllName);

        if (!File.Exists(hookDll))
        {
            Console.Error.WriteLine($"ERROR: no startup hook found at {hookDll}.");
            Console.Error.WriteLine("  The hook is installed and updated by the in-game osu-cc updater plugin; on a fresh");
            Console.Error.WriteLine("  install download the latest bundle from");
            Console.Error.WriteLine("    https://github.com/rus07tam/osu-cc/releases/latest");
            Console.Error.WriteLine("  and extract its hook/ folder to the osu-cc data root (see 'osucc status').");
            return 1;
        }

        StagedUpdateApplier.Apply(paths);
        return GameLauncher.Launch(paths.OsuDirectory, hookDll);
    }
}
