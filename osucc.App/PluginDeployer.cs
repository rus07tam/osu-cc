using System;
using System.IO;

namespace osucc.App;

/// <summary>
/// Copies built plugin archives (<c>&lt;Name&gt;.zip</c>) from each plugin project's output
/// into the osu-cc data root's <c>plugins</c> folder.
/// </summary>
internal static class PluginDeployer
{
    /// <summary>Copies every archive that exists in the given plugin project output folders.</summary>
    public static void Deploy(string repoRoot, string config, string pluginsDirectory)
    {
        Directory.CreateDirectory(pluginsDirectory);

        foreach (string project in PluginDiscovery.DiscoverProjects(repoRoot))
        {
            string name = Path.GetFileNameWithoutExtension(project);
            string source = Path.Combine(PluginDiscovery.OutputDirectory(repoRoot, config, project), $"{name}.zip");

            if (!File.Exists(source))
            {
                Console.WriteLine($"Skipped {name}.zip (not built - run 'osucc build').");
                continue;
            }

            File.Copy(source, Path.Combine(pluginsDirectory, $"{name}.zip"), overwrite: true);
            Console.WriteLine($"Deployed plugin archive: {name}.zip");
        }
    }
}
