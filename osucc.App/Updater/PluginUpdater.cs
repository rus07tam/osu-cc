using System.Text.Json;

namespace osucc.App.Updater;

/// <summary>
/// Downloads the shipped plugin archives (zip assets) from the latest GitHub release of the
/// osu-cc repo into the plugins folder; the in-game <c>PluginPackageStore</c> unpacks them on
/// the next launch. Third-party plugins in the folder are left alone.
/// </summary>
internal static class PluginUpdater
{
    private const string repository = "rus07tam/osu-cc";
    private const string latestReleaseUrl = $"https://api.github.com/repos/{repository}/releases/latest";

    public static async Task<int> UpdateAsync(HttpClient http, string pluginsDirectory)
    {
        using JsonDocument? release = await GetLatestReleaseAsync(http);

        if (release == null)
        {
            Console.Error.WriteLine("ERROR: cannot reach GitHub to check the latest release.");
            return 1;
        }

        string tag = release.RootElement.GetProperty("tag_name").GetString() ?? string.Empty;

        if (IsCurrent(pluginsDirectory, tag))
        {
            Console.WriteLine($"Plugins already up to date ({tag}).");
            return 0;
        }

        Directory.CreateDirectory(pluginsDirectory);

        foreach (JsonElement asset in release.RootElement.GetProperty("assets").EnumerateArray())
        {
            string name = asset.GetProperty("name").GetString() ?? string.Empty;

            if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                continue;

            string url = asset.GetProperty("browser_download_url").GetString() ?? string.Empty;

            if (string.IsNullOrEmpty(url))
                continue;

            string target = Path.Combine(pluginsDirectory, name);

            using HttpResponseMessage response = await http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"ERROR: failed to download {name}.");
                return 1;
            }

            await using (FileStream stream = File.Create(target))
                await response.Content.CopyToAsync(stream);

            Console.WriteLine($"Downloaded plugin archive: {name}");
        }

        WriteMarker(pluginsDirectory, tag);
        return 0;
    }

    /// <summary>True when the latest release was already fetched via <c>osucc update</c>.</summary>
    private static bool IsCurrent(string pluginsDirectory, string tag)
    {
        string markerFile = MarkerPath(pluginsDirectory);
        return File.Exists(markerFile) && File.ReadAllText(markerFile).Trim() == tag;
    }

    private static void WriteMarker(string pluginsDirectory, string tag)
        => File.WriteAllText(MarkerPath(pluginsDirectory), tag);

    // Marker lives in the osu-cc data root, next to the plugins folder.
    private static string MarkerPath(string pluginsDirectory)
        => Path.Combine(Path.GetDirectoryName(pluginsDirectory) ?? string.Empty, "osucc.plugins-version");

    private static async Task<JsonDocument?> GetLatestReleaseAsync(HttpClient http)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, latestReleaseUrl);
        request.Headers.UserAgent.ParseAdd("osucc-updater");

        HttpResponseMessage response = await http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }
}
