using osucc.Common.GitHub;
using System.IO.Compression;

namespace osucc.Common.Update;

public enum UpdateStage
{
    Checking,
    Downloading,
    Extracting,
    Applying,
    Done,
    Failed,
}

public sealed class OsuCcUpdateService : IDisposable
{
    public const string DefaultRepository = "RuJect/osu-cc";

    private readonly GitHubReleasesClient github;
    private readonly string osuCcDirectory;
    public string Repository { get; set; }

    public OsuCcUpdateService(string osuCcDirectory, string repository = DefaultRepository)
    {
        this.osuCcDirectory = osuCcDirectory;
        Repository = repository;
        github = new GitHubReleasesClient();
    }

    public string? InstalledVersion => OsuCcVersionReader.Read(Path.Combine(osuCcDirectory, OsuCcLayout.HookDirectoryName, OsuCcLayout.HookDllName));
    public bool IsInstalled => !string.IsNullOrEmpty(InstalledVersion);

    public async Task<string?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        GitHubRelease? release = await github.FindReleaseWithAssetAsync(
            Repository,
            name => name.StartsWith(OsuCcLayout.RuntimeBundlePrefix, StringComparison.Ordinal) && name.EndsWith(".zip", StringComparison.Ordinal),
            ct: ct).ConfigureAwait(false);

        if (release == null) return null;

        string version = NormaliseVersion(release.TagName);
        string? installed = InstalledVersion;

        if (installed != null && OsuCcVersionReader.IsAtLeast(installed, version))
            return null;

        return version;
    }

    public async Task UpdateAsync(IProgress<(UpdateStage Stage, float Progress, string? ErrorReason)> progress, CancellationToken ct = default)
    {
        try
        {
            progress.Report((UpdateStage.Checking, 0f, null));

            GitHubRelease? release = await github.FindReleaseWithAssetAsync(
                Repository,
                name => name.StartsWith(OsuCcLayout.RuntimeBundlePrefix, StringComparison.Ordinal) && name.EndsWith(".zip", StringComparison.Ordinal),
                ct: ct).ConfigureAwait(false);

            if (release == null)
            {
                progress.Report((UpdateStage.Failed, 0f, "No release found"));
                return;
            }

            GitHubAsset? asset = release.Assets.FirstOrDefault(
                a => a.Name.StartsWith(OsuCcLayout.RuntimeBundlePrefix, StringComparison.Ordinal) && a.Name.EndsWith(".zip", StringComparison.Ordinal));

            if (asset == null)
            {
                progress.Report((UpdateStage.Failed, 0f, "No valid asset in release"));
                return;
            }

            progress.Report((UpdateStage.Downloading, 0f, null));

            string? tempFile = await github.DownloadAssetAsync(asset.DownloadUrl, ct).ConfigureAwait(false);

            if (tempFile == null)
            {
                progress.Report((UpdateStage.Failed, 0f, "Download failed"));
                return;
            }

            try
            {
                progress.Report((UpdateStage.Extracting, 0f, null));

                string hookDir = Path.Combine(osuCcDirectory, OsuCcLayout.HookDirectoryName);
                Directory.CreateDirectory(hookDir);

                using (ZipArchive archive = ZipFile.OpenRead(tempFile))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.Length == 0 || entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
                            continue;

                        string? entryDir = Path.GetDirectoryName(entry.FullName);
                        bool isInHook = string.Equals(entryDir, "hook", StringComparison.OrdinalIgnoreCase)
                            || string.IsNullOrEmpty(entryDir);

                        if (!isInHook)
                            continue;

                        string destPath = Path.Combine(hookDir, Path.GetFileName(entry.FullName));
                        entry.ExtractToFile(destPath, overwrite: true);
                    }
                }

                progress.Report((UpdateStage.Applying, 0.5f, null));
                progress.Report((UpdateStage.Done, 1f, null));
            }
            finally
            {
                try { File.Delete(tempFile); } catch (Exception) { }
            }
        }
        catch (Exception ex)
        {
            progress.Report((UpdateStage.Failed, 0f, ex.Message));
        }
    }

    public async Task InstallAsync(IProgress<(UpdateStage Stage, float Progress, string? ErrorReason)> progress, CancellationToken ct = default)
    {
        try
        {
            progress.Report((UpdateStage.Checking, 0f, null));

            GitHubRelease? release = await github.FindReleaseWithAssetAsync(
                Repository,
                name => name.StartsWith(OsuCcLayout.BootstrapBundlePrefix, StringComparison.Ordinal) && name.EndsWith(".zip", StringComparison.Ordinal),
                ct: ct).ConfigureAwait(false);

            if (release == null)
            {
                progress.Report((UpdateStage.Failed, 0f, "No release found"));
                return;
            }

            GitHubAsset? asset = release.Assets.FirstOrDefault(
                a => a.Name.StartsWith(OsuCcLayout.BootstrapBundlePrefix, StringComparison.Ordinal) && a.Name.EndsWith(".zip", StringComparison.Ordinal));

            if (asset == null)
            {
                progress.Report((UpdateStage.Failed, 0f, "No valid asset in release"));
                return;
            }

            progress.Report((UpdateStage.Downloading, 0f, null));

            string? tempFile = await github.DownloadAssetAsync(asset.DownloadUrl, ct).ConfigureAwait(false);

            if (tempFile == null)
            {
                progress.Report((UpdateStage.Failed, 0f, "Download failed"));
                return;
            }

            try
            {
                progress.Report((UpdateStage.Extracting, 0f, null));

                if (Directory.Exists(osuCcDirectory))
                    Directory.Delete(osuCcDirectory, recursive: true);

                Directory.CreateDirectory(osuCcDirectory);

                ZipFile.ExtractToDirectory(tempFile, osuCcDirectory, overwriteFiles: true);

                progress.Report((UpdateStage.Applying, 0.5f, null));
                progress.Report((UpdateStage.Done, 1f, null));
            }
            finally
            {
                try { File.Delete(tempFile); } catch (Exception) { }
            }
        }
        catch (Exception ex)
        {
            progress.Report((UpdateStage.Failed, 0f, ex.Message));
        }
    }

    public Task UninstallAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            if (Directory.Exists(osuCcDirectory))
                Directory.Delete(osuCcDirectory, recursive: true);
        }, ct);
    }

    public void Dispose() => github.Dispose();

    private static string NormaliseVersion(string tag)
        => tag.StartsWith('v') || tag.StartsWith('V') ? tag[1..] : tag;
}
