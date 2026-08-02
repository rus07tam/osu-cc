using System.Text.Json;

namespace osucc.App.Updater;

/// <summary>Minimal NuGet client for the flat-container API (version index + nupkg download).</summary>
internal static class NuGetFetcher
{
    private const string flatContainer = "https://api.nuget.org/v3-flatcontainer";

    /// <summary>Latest stable (non-prerelease) version of a package, or null when the feed is unreachable.</summary>
    public static async Task<string?> LatestStableVersionAsync(HttpClient http, string packageId)
    {
        string id = packageId.ToLowerInvariant();

        using HttpResponseMessage response = await http.GetAsync($"{flatContainer}/{id}/index.json");

        if (!response.IsSuccessStatusCode)
            return null;

        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        string? latest = null;

        foreach (JsonElement element in document.RootElement.GetProperty("versions").EnumerateArray())
        {
            string version = element.GetString() ?? string.Empty;

            if (version.Contains('-') || !Version.TryParse(version, out Version? parsed))
                continue;

            if (latest == null || parsed > Version.Parse(latest))
                latest = version;
        }

        return latest;
    }

    /// <summary>Downloads a package into <paramref name="targetDirectory"/>, returning the nupkg path or null on failure.</summary>
    public static async Task<string?> DownloadPackageAsync(HttpClient http, string packageId, string version, string targetDirectory)
    {
        string id = packageId.ToLowerInvariant();
        string fileName = $"{id}.{version}.nupkg";

        using HttpResponseMessage response = await http.GetAsync($"{flatContainer}/{id}/{version}/{fileName}");

        if (!response.IsSuccessStatusCode)
            return null;

        Directory.CreateDirectory(targetDirectory);
        string path = Path.Combine(targetDirectory, fileName);

        await using (FileStream stream = File.Create(path))
            await response.Content.CopyToAsync(stream);

        return path;
    }
}
