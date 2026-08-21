using System;
using System.Diagnostics;
using System.IO;

namespace osucc.Launcher.Core
{
    /// <summary>
    /// Launches osu! with <c>DOTNET_STARTUP_HOOKS</c> set only on the child process, so the hook
    /// is active solely when started through osucc.
    /// </summary>
    public static class GameLauncher
    {
        public static Process? Launch(string osuDirectory, string hookDll)
        {
            string executable = OsuCcPaths.ResolveExecutable(osuDirectory);

            if (!File.Exists(executable))
                return null;

            if (!File.Exists(hookDll))
                return null;

            var startInfo = new ProcessStartInfo(executable)
            {
                WorkingDirectory = osuDirectory,
                UseShellExecute = false,
            };

            startInfo.Environment["DOTNET_STARTUP_HOOKS"] = hookDll;

            try
            {
                var process = new Process { StartInfo = startInfo };
                process.EnableRaisingEvents = true;
                process.Start();
                return process;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start osu!: {ex.Message}");
                return null;
            }
        }
    }
}
