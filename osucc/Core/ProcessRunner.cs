using System.Diagnostics;

namespace osucc.App;

/// <summary>Runs a child process and waits for it, returning its exit code (1 when the process could not be started).</summary>
internal static class ProcessRunner
{
    public static int Run(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName) { UseShellExecute = false };

        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        return Run(startInfo);
    }

    public static int Run(ProcessStartInfo startInfo)
    {
        using var process = Process.Start(startInfo);

        if (process == null)
        {
            Console.Error.WriteLine($"ERROR: failed to start {startInfo.FileName}.");
            return 1;
        }

        process.WaitForExit();
        return process.ExitCode;
    }
}
