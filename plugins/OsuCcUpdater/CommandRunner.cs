using System.Diagnostics;
using System.Text;

namespace OsuCcUpdater
{
    public readonly record struct CommandResult(bool Ok, string? ErrorTail);

    /// <summary>
    /// Runs external commands (git, dotnet) with output streaming: every line is forwarded to
    /// <paramref name="onOutput"/> as it arrives (for the plugin log) while a bounded buffer keeps
    /// the tail for a useful error message when the command fails.
    /// </summary>
    internal static class CommandRunner
    {
        /// <summary>Maximum lines retained so a failed build reports its tail instead of megabytes.</summary>
        private const int maxBufferedLines = 200;

        public static async Task<CommandResult> RunAsync(
            string fileName,
            IEnumerable<string> arguments,
            string workingDirectory,
            Action<string>? onOutput,
            CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo(fileName)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            foreach (string argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo);

            if (process == null)
                return new CommandResult(false, $"could not start {fileName}");

            var buffered = new LinkedList<string>();

            void capture(string? line)
            {
                if (string.IsNullOrEmpty(line))
                    return;

                buffered.AddLast(line);

                while (buffered.Count > maxBufferedLines)
                    buffered.RemoveFirst();

                onOutput?.Invoke(line);
            }

            Task outTask = Task.Run(() =>
            {
                string? line;

                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    capture(line);
                }
            }, cancellationToken);

            Task errTask = Task.Run(() =>
            {
                string? line;

                while ((line = process.StandardError.ReadLine()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    capture(line);
                }
            }, cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(true);
                }
                catch (Exception)
                {
                    // best effort
                }

                await Task.WhenAll(outTask, errTask).ConfigureAwait(false);
                return new CommandResult(false, "cancelled");
            }

            await Task.WhenAll(outTask, errTask).ConfigureAwait(false);

            string tail = buffered.Count > 0
                ? string.Join(Environment.NewLine, buffered)
                : $"exited with code {process.ExitCode}";

            return new CommandResult(process.ExitCode == 0, tail);
        }
    }
}