using System.Text.Json;

namespace OsuCcUpdater
{
    /// <summary>
    /// Talks to the GitHub Releases API (official osu-cc repo) to find the latest release and its
    /// <c>osucc-runtime-&lt;version&gt;.zip</c> asset, then downloads it. Unauthenticated API calls
    /// are rate limited (60/hour), so callers throttle with a persisted last-check timestamp.
    /// </summary>
    internal sealed class GithubBundleSource
    {
        private const string repo = "rus07tam/osu-cc";
        private const string latestUrl = $"https://api.github.com/repos/{repo}/releases/latest";

        private readonly HttpClient http;
        private readonly Action<string>? log;

        public GithubBundleSource(HttpClient http, Action<string>? log)
        {
            this.http = http;
            this.log = log;
        }

        /// <summary>
        /// The latest release's tag and the download URL of its runtime bundle asset.
        /// <paramref name="tag"/> is <c>null</c> on failure; <paramref name="error"/> explains why.
        /// </summary>
        public async Task<(string? Tag, string? Url, string? Error)> QueryLatestAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var response = await http.GetAsync(latestUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return (null, null, $"GitHub API returned {(int)response.StatusCode}{rateLimitTip(response)}");

                using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                JsonElement root = document.RootElement;

                string? tag = root.TryGetProperty("tag_name", out JsonElement tagElement) ? tagElement.GetString() : null;

                if (string.IsNullOrEmpty(tag))
                    return (null, null, "release has no tag");

                string? url = null;

                if (root.TryGetProperty("assets", out JsonElement assets))
                {
                    foreach (JsonElement asset in assets.EnumerateArray())
                    {
                        string? name = asset.TryGetProperty("name", out JsonElement nameElement) ? nameElement.GetString() : null;

                        if (name != null && name.StartsWith("osucc-runtime-", StringComparison.Ordinal) && name.EndsWith(".zip", StringComparison.Ordinal))
                        {
                            url = asset.TryGetProperty("browser_download_url", out JsonElement urlElement) ? urlElement.GetString() : null;
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(url))
                    return (tag, null, "the release has no osucc-runtime bundle asset");

                return (tag, url, null);
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return (null, null, $"{ex.Message}");
            }
        }

        /// <summary>Downloads the bundle to a temp file and returns its path (deleted by the stage step).</summary>
        public async Task<string?> DownloadAsync(string url, CancellationToken cancellationToken)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"osucc-runtime-{Guid.NewGuid():N}.zip");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Accept.ParseAdd("application/octet-stream");

                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    log?.Invoke($"download failed: GitHub returned {(int)response.StatusCode}");
                    return null;
                }

                await using (Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                await using (FileStream target = File.Create(tempFile))
                {
                    await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
                }

                return tempFile;
            }
            catch (Exception ex)
            {
                log?.Invoke($"download failed: {ex.Message}");

                try
                {
                    File.Delete(tempFile);
                }
                catch (IOException)
                {
                }

                return null;
            }
        }

        private static string rateLimitTip(HttpResponseMessage response)
            => response.StatusCode == System.Net.HttpStatusCode.Forbidden || response.StatusCode == (System.Net.HttpStatusCode)429
                ? " (GitHub API rate limit - try again later or use a local build)"
                : string.Empty;
    }
}