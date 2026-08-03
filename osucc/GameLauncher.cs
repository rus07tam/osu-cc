using System.Diagnostics;

namespace osucc.App;

/// <summary>
/// Launches osu! with <c>DOTNET_STARTUP_HOOKS</c> set only on the child process, so the hook
/// is active solely when started through osucc.
/// </summary>
internal static class GameLauncher
{
    /// <summary>Returns the osu! exit code, or a non-zero code when the executable is missing.</summary>
    public static int Launch(string osuDirectory, string hookDll)
    {
        string executable = OsuCcPaths.ResolveExecutable(osuDirectory);

        if (!File.Exists(executable))
        {
            Console.Error.WriteLine($"ERROR: {executable} not found. Pass --osu-dir to point at the install.");
            return 1;
        }

        if (!File.Exists(hookDll))
        {
            Console.Error.WriteLine($"ERROR: {hookDll} not found. Run 'osucc update' or 'osucc start' first.");
            return 1;
        }

        var startInfo = new ProcessStartInfo(executable)
        {
            WorkingDirectory = osuDirectory,
            UseShellExecute = false,
        };

        startInfo.Environment["DOTNET_STARTUP_HOOKS"] = hookDll;

        Console.WriteLine($"Launching {executable} with DOTNET_STARTUP_HOOKS={hookDll}");

        return ProcessRunner.Run(startInfo);
    }
}
