using System.Text.Json;

namespace osucc.App.Updater;

/// <summary>
/// Downloads the shipped plugin archives (zip assets) from the latest GitHub release of the
/// osu-cc repo into the plugins folder; the in-game <c>PluginPackageStore</c> unpacks them on
/// the next launch. Every run fetches the archives again (a few hundred KB, idempotent), so no
/// marker file is needed; a release that ships no plugin archives is treated as an error rather
/// than a silent no-op. Third-party plugins in the folder are left alone.
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

        var archives = release.RootElement.GetProperty("assets").EnumerateArray()
            .Where(a => (a.GetProperty("name").GetString() ?? string.Empty).EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (archives.Count == 0)
        {
            Console.Error.WriteLine($"ERROR: the latest release ({tag}) has no plugin archives.");
            return 1;
        }

        Directory.CreateDirectory(pluginsDirectory);

        foreach (JsonElement asset in archives)
        {
            string name = asset.GetProperty("name").GetString() ?? string.Empty;
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

        return 0;
    }

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
