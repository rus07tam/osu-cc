using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace osucc.App;

/// <summary>
/// Finds plugin projects under <c>plugins/</c> by convention: one project per folder, named
/// after it (<c>plugins/&lt;Name&gt;/&lt;Name&gt;.csproj</c>). Discovery replaces the old hardcoded
/// registry, so adding a plugin needs no launcher change.
/// </summary>
internal static class PluginDiscovery
{
    public static IEnumerable<string> DiscoverProjects(string repoRoot)
        => Directory.EnumerateDirectories(Path.Combine(repoRoot, "plugins"))
                    .Select(dir => Path.Combine(dir, Path.GetFileName(dir) + ".csproj"))
                    .Where(File.Exists)
                    .OrderBy(Path.GetFileNameWithoutExtension, System.StringComparer.Ordinal);

    public static string OutputDirectory(string repoRoot, string config, string project)
    {
        string name = Path.GetFileNameWithoutExtension(project);
        return Path.Combine(repoRoot, "plugins", name, "bin", config, "net8.0");
    }
}
