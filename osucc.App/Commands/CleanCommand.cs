using DotMake.CommandLine;

namespace osucc.App.Commands;

/// <summary>Removes hook files from the osu install dir (legacy deployment) and the osu-cc data root.</summary>
[CliCommand(Description = "Remove hook files from the osu install dir (legacy deployment) and the osu-cc data root.")]
public class CleanCommand
{
    public RootCliCommand Root { get; set; } = null!;

    // Only our two blobs are cleaned. SharpCompress.dll is a production dependency of
    // osu.Game.dll (0.49.1) and must never be touched — it ships with the game itself.
    private static readonly string[] hookBlobNames = { "osucc.dll", "0Harmony.dll" };

    public int Run()
    {
        var paths = Root.ResolvePaths();
        int removed = 0;

        foreach (string name in hookBlobNames)
        {
            string path = Path.Combine(paths.OsuDirectory, name);

            if (!File.Exists(path))
                continue;

            try
            {
                File.Delete(path);
                Console.WriteLine($"Removed {path}");
                removed++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: cannot remove {path}: {ex.Message} - close osu! first.");
            }
        }

        if (Directory.Exists(paths.HookDirectory))
        {
            try
            {
                Directory.Delete(paths.HookDirectory, recursive: true);
                Console.WriteLine($"Removed {paths.HookDirectory}");
                removed++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: cannot remove {paths.HookDirectory}: {ex.Message}");
            }
        }

        Console.WriteLine(removed == 0 ? "Nothing to clean." : "Cleanup done.");
        return 0;
    }
}
