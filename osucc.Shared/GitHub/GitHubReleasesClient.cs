using System.Net.Http.Headers;
using System.Text.Json;

namespace osucc.Common.GitHub;

public sealed class GitHubAsset
{
    public string Name { get; init; } = string.Empty;
    public string DownloadUrl { get; init; } = string.Empty;
    public long Size { get; init; }
}

public sealed class GitHubRelease
{
    public string TagName { get; init; } = string.Empty;
    public List<GitHubAsset> Assets { get; init; } = new();
}

public sealed class GitHubReleasesClient : IDisposable
{
    private readonly HttpClient http;

    public GitHubReleasesClient()
    {
        http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("osucc");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<GitHubRelease?> GetLatestReleaseAsync(string repo, CancellationToken ct = default)
    {
        try
        {
            using HttpResponseMessage response = await http
                .GetAsync($"https://api.github.com/repos/{repo}/releases/latest", ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            string json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            return parseRelease(doc.RootElement);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<GitHubRelease?> FindReleaseWithAssetAsync(
        string repo,
        Func<string, bool> assetPredicate,
        int maxPages = 5,
        CancellationToken ct = default)
    {
        for (int page = 1; page <= maxPages; page++)
        {
            List<GitHubRelease> releases = await fetchReleasesPageAsync(repo, page, ct).ConfigureAwait(false);

            if (releases.Count == 0)
                return null;

            foreach (GitHubRelease release in releases)
            {
                if (release.Assets.Any(a => assetPredicate(a.Name)))
                    return release;
            }
        }

        return null;
    }

    public async Task<List<GitHubRelease>> FindReleasesWithAssetAsync(
        string repo,
        Func<string, bool> assetPredicate,
        int limit,
        int maxPages = 10,
        CancellationToken ct = default)
    {
        var results = new List<GitHubRelease>();

        for (int page = 1; page <= maxPages; page++)
        {
            List<GitHubRelease> releases = await fetchReleasesPageAsync(repo, page, ct).ConfigureAwait(false);

            if (releases.Count == 0)
                break;

            foreach (GitHubRelease release in releases)
            {
                if (release.Assets.Any(a => assetPredicate(a.Name)))
                {
                    results.Add(release);

                    if (results.Count >= limit)
                        return results;
                }
            }
        }

        return results;
    }

    public async Task<string?> DownloadAssetAsync(string downloadUrl, CancellationToken ct = default)
    {
        try
        {
            string tempFile = Path.GetTempFileName();

            using HttpResponseMessage response = await http
                .GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                File.Delete(tempFile);
                return null;
            }

            await using (Stream src = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            await using (var dst = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await src.CopyToAsync(dst, ct).ConfigureAwait(false);
            }

            return tempFile;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void Dispose() => http.Dispose();

    private async Task<List<GitHubRelease>> fetchReleasesPageAsync(string repo, int page, CancellationToken ct)
    {
        try
        {
            using HttpResponseMessage response = await http
                .GetAsync($"https://api.github.com/repos/{repo}/releases?per_page=20&page={page}", ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return new List<GitHubRelease>();

            string json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            return parseReleases(doc.RootElement);
        }
        catch (Exception)
        {
            return new List<GitHubRelease>();
        }
    }

    private static GitHubRelease? parseRelease(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return null;

        string tagName = root.TryGetProperty("tag_name", out JsonElement tagEl)
            ? tagEl.GetString() ?? string.Empty
            : string.Empty;

        var assets = new List<GitHubAsset>();

        if (root.TryGetProperty("assets", out JsonElement assetsEl) && assetsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement assetEl in assetsEl.EnumerateArray())
            {
                string name = assetEl.TryGetProperty("name", out JsonElement nameEl)
                    ? nameEl.GetString() ?? string.Empty
                    : string.Empty;

                string downloadUrl = assetEl.TryGetProperty("browser_download_url", out JsonElement urlEl)
                    ? urlEl.GetString() ?? string.Empty
                    : string.Empty;

                long size = assetEl.TryGetProperty("size", out JsonElement sizeEl)
                    ? sizeEl.GetInt64()
                    : 0;

                assets.Add(new GitHubAsset { Name = name, DownloadUrl = downloadUrl, Size = size });
            }
        }

        return new GitHubRelease { TagName = tagName, Assets = assets };
    }

    private static List<GitHubRelease> parseReleases(JsonElement root)
    {
        var releases = new List<GitHubRelease>();

        if (root.ValueKind != JsonValueKind.Array)
            return releases;

        foreach (JsonElement element in root.EnumerateArray())
        {
            GitHubRelease? release = parseRelease(element);
            if (release != null)
                releases.Add(release);
        }

        return releases;
    }
}
