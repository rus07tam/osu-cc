using System.IO.Compression;
using System.Reflection;
using System.Xml.Linq;

namespace osucc.App.Updater;

/// <summary>
/// Deploys the latest hook into the hook folder straight from nuget.org: downloads the
/// <c>osucc.Host</c> package and the runtime dependencies the hook loads next to it
/// (<c>Lib.Harmony</c> → <c>0Harmony.dll</c>, <c>SharpCompress</c>), mirroring what a local
/// build plus <c>HookDeployer</c> would copy. The osu install dir is never touched.
/// </summary>
internal static class HookUpdater
{
    private const string packageId = "osucc.Host";

    // Runtime dependencies that must sit next to osucc.dll in the hook folder. Their versions
    // are read from the osucc.Host nuspec so they always match the published package.
    private static readonly (string Id, string FileName)[] runtimeDependencies =
    {
        ("Lib.Harmony", "0Harmony.dll"),
        ("SharpCompress", "SharpCompress.dll"),
    };

    public static async Task<int> UpdateAsync(HttpClient http, string hookDirectory)
    {
        string? latest = await NuGetFetcher.LatestStableVersionAsync(http, packageId);

        if (latest == null)
        {
            Console.Error.WriteLine("ERROR: cannot reach nuget.org to check the hook version.");
            return 1;
        }

        if (IsCurrent(hookDirectory, latest))
        {
            Console.WriteLine($"Hook already up to date (osucc.Host {latest}).");
            return 0;
        }

        string tempDirectory = Path.Combine(Path.GetTempPath(), $"osucc-update-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDirectory);
            string? package = await NuGetFetcher.DownloadPackageAsync(http, packageId, latest, tempDirectory);

            if (package == null)
            {
                Console.Error.WriteLine($"ERROR: failed to download {packageId} {latest}.");
                return 1;
            }

            // The hook assembly itself.
            if (!ExtractEntry(package, e => e.EndsWith("lib/net8.0/osucc.dll", StringComparison.Ordinal), Path.Combine(tempDirectory, "osucc.dll")))
            {
                Console.Error.WriteLine($"ERROR: {packageId} {latest} does not contain the hook assembly.");
                return 1;
            }

            // Runtime dependencies, versions taken from the package's own nuspec.
            XDocument nuspec = ReadNuspec(package);

            foreach ((string id, string fileName) in runtimeDependencies)
            {
                string? version = FindDependencyVersion(nuspec, id);

                if (version == null)
                {
                    Console.Error.WriteLine($"ERROR: {packageId} {latest} does not declare the {id} dependency.");
                    return 1;
                }

                string? dependencyPackage = await NuGetFetcher.DownloadPackageAsync(http, id, version, tempDirectory);

                if (dependencyPackage == null || !ExtractBestTfmDll(dependencyPackage, Path.Combine(tempDirectory, fileName)))
                {
                    Console.Error.WriteLine($"ERROR: failed to fetch {id} {version}.");
                    return 1;
                }
            }

            Directory.CreateDirectory(hookDirectory);

            foreach (string file in new[] { "osucc.dll", "0Harmony.dll", "SharpCompress.dll" })
                File.Copy(Path.Combine(tempDirectory, file), Path.Combine(hookDirectory, file), overwrite: true);

            Console.WriteLine($"Hook updated to {latest}.");
            return 0;
        }
        finally
        {
            try
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>True when the deployed hook assembly already carries the latest version.</summary>
    private static bool IsCurrent(string hookDirectory, string latest)
    {
        string hookDll = Path.Combine(hookDirectory, "osucc.dll");

        if (!File.Exists(hookDll) || !Version.TryParse(latest, out Version? latestVersion))
            return false;

        Version? deployed = AssemblyName.GetAssemblyName(hookDll).Version;
        return deployed != null && deployed >= latestVersion;
    }

    private static bool ExtractEntry(string packagePath, Func<string, bool> match, string targetPath)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(e => match(e.FullName));

        if (entry == null)
            return false;

        entry.ExtractToFile(targetPath, overwrite: true);
        return true;
    }

    /// <summary>Extracts the single assembly from the highest-ranked <c>lib/&lt;tfm&gt;/</c> folder.</summary>
    private static bool ExtractBestTfmDll(string packagePath, string targetPath)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        ZipArchiveEntry? best = null;
        int bestRank = -1;

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            // lib/<tfm>/<name>.dll
            string[] parts = entry.FullName.Split('/');

            if (parts.Length < 3 || !parts[0].Equals("lib", StringComparison.Ordinal) || !parts[2].EndsWith(".dll", StringComparison.Ordinal))
                continue;

            int rank = RankTfm(parts[1]);

            if (rank > bestRank)
            {
                bestRank = rank;
                best = entry;
            }
        }

        if (best == null)
            return false;

        best.ExtractToFile(targetPath, overwrite: true);
        return true;
    }

    private static int RankTfm(string tfm) => tfm switch
    {
        "net8.0" => 6,
        "net9.0" or "net10.0" => 5,
        "net6.0" or "net7.0" => 4,
        "net5.0" => 3,
        "netstandard2.1" => 2,
        _ when tfm.StartsWith("netstandard", StringComparison.Ordinal) => 1,
        _ => 0,
    };

    private static XDocument ReadNuspec(string packagePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        ZipArchiveEntry? nuspec = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec", StringComparison.Ordinal));

        using Stream stream = (nuspec ?? throw new InvalidDataException("package contains no nuspec")).Open();
        return XDocument.Load(stream);
    }

    private static string? FindDependencyVersion(XDocument nuspec, string dependencyId)
    {
        XNamespace ns = nuspec.Root?.Name.Namespace ?? XNamespace.None;
        return nuspec.Descendants(ns + "dependency")
            .Where(d => (string?)d.Attribute("id") == dependencyId)
            .Select(d => (string?)d.Attribute("version"))
            .FirstOrDefault();
    }
}
