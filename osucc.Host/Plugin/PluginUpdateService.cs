using osucc.Common;
using osucc.Common.GitHub;

namespace osucc.Plugin;

public sealed class PluginVersionInfo
{
    public string Version { get; init; } = string.Empty;
    public string DownloadUrl { get; init; } = string.Empty;
    public DateTimeOffset PublishedAt { get; init; }
}

public sealed class PluginUpdateService : IDisposable
{
    public static PluginUpdateService? Instance { get; private set; }

    private readonly GitHubReleasesClient github;
    private readonly string pluginsDirectory;

    public PluginUpdateService(string pluginsDirectory)
    {
        this.pluginsDirectory = pluginsDirectory;
        github = new GitHubReleasesClient();
        Instance = this;
    }

    public async Task<PluginVersionInfo?> CheckUpdateAsync(PluginEntry entry, CancellationToken ct = default)
    {
        string? repo = ParseRepo(entry.Repository);
        if (repo == null) return null;

        string prefix = OsuCcLayout.PluginArchivePrefix(entry.Id);
        var release = await github.FindReleaseWithAssetAsync(repo, name => name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".zip", StringComparison.Ordinal), ct: ct).ConfigureAwait(false);
        if (release == null) return null;

        var asset = release.Assets.First(a => a.Name.StartsWith(prefix, StringComparison.Ordinal));
        string version = ExtractVersion(asset.Name, prefix);

        if (!string.IsNullOrEmpty(entry.Version) && OsuCcVersionReader.IsAtLeast(entry.Version, version))
            return null;

        return new PluginVersionInfo { Version = version, DownloadUrl = asset.DownloadUrl };
    }

    public async Task<List<PluginVersionInfo>> GetAvailableVersionsAsync(PluginEntry entry, int limit = 10, CancellationToken ct = default)
    {
        string? repo = ParseRepo(entry.Repository);
        if (repo == null) return new List<PluginVersionInfo>();

        string prefix = OsuCcLayout.PluginArchivePrefix(entry.Id);
        var releases = await github.FindReleasesWithAssetAsync(repo, name => name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".zip", StringComparison.Ordinal), limit, ct: ct).ConfigureAwait(false);

        return releases.Select(r =>
        {
            var asset = r.Assets.First(a => a.Name.StartsWith(prefix, StringComparison.Ordinal));
            return new PluginVersionInfo
            {
                Version = ExtractVersion(asset.Name, prefix),
                DownloadUrl = asset.DownloadUrl,
            };
        }).ToList();
    }

    public async Task InstallVersionAsync(PluginEntry entry, PluginVersionInfo version, CancellationToken ct = default)
    {
        string? tempFile = await github.DownloadAssetAsync(version.DownloadUrl, ct).ConfigureAwait(false);
        if (tempFile == null) return;

        string dest = Path.Combine(pluginsDirectory, OsuCcLayout.PluginArchiveName(entry.Id, version.Version));
        File.Move(tempFile, dest, overwrite: true);
    }

    public void Dispose()
    {
        github.Dispose();
        if (ReferenceEquals(Instance, this))
            Instance = null;
    }

    internal static string? ParseRepo(string? repository)
    {
        if (string.IsNullOrEmpty(repository)) return null;
        if (repository.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
            return repository["https://github.com/".Length..].TrimEnd('/');
        if (repository.StartsWith("http://github.com/", StringComparison.OrdinalIgnoreCase))
            return repository["http://github.com/".Length..].TrimEnd('/');
        if (repository.Contains('/') && !repository.Contains(' '))
            return repository;
        return null;
    }

    private static string ExtractVersion(string assetName, string prefix)
    {
        string withoutPrefix = assetName[prefix.Length..];
        return withoutPrefix.EndsWith(".zip", StringComparison.Ordinal)
            ? withoutPrefix[..^4]
            : withoutPrefix;
    }
}
