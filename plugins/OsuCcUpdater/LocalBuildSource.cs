using osucc.Common;

namespace OsuCcUpdater
{
    // CA1861 prefers cached arrays over fresh `new[]` literals; these are one-off process
    // invocations where the allocation is irrelevant, so the rule is disabled for this file.
#pragma warning disable CA1861

    /// <summary>
    /// The local-build source: clones the official osu-cc repository (or updates an existing clone)
    /// into <c>&lt;osu-cc&gt;/src/osu-cc</c>, checks out the newest version tag and runs the same
    /// <c>osucc.build.proj -t:PackBootstrapBundle</c> pipeline the CI uses, producing a runtime
    /// bundle next to the local build. Requires <c>git</c> and the .NET SDK on PATH.
    /// </summary>
    internal sealed class LocalBuildSource
    {
        private const string officialUrl = "https://github.com/rus07tam/osu-cc.git";

        private readonly string repoDirectory;
        private readonly Action<string>? log;

        public LocalBuildSource(string osuCcDirectory, Action<string>? log)
        {
            repoDirectory = Path.Combine(osuCcDirectory, "src", OsuCcLayout.OsuCcDirectoryName);
            this.log = log;
        }

        /// <summary>Path of the official repo clone under the osu-cc data root.</summary>
        public string RepoDirectory => repoDirectory;

        /// <summary>Last failure explanation (set when a method returns <c>null</c>).</summary>
        public string? LastError { get; private set; }

        /// <summary>
        /// Ensures a fresh clone of the official repo, fetches the newest tags and returns the
        /// latest version tag (e.g. <c>v1.0.0</c>), or <c>null</c> on failure.
        /// </summary>
        public async Task<string?> ResolveLatestTagAsync(CancellationToken cancellationToken)
        {
            if (!await ensureOfficialRepoAsync(cancellationToken))
                return null;

            var tags = new List<string>();

            CommandResult result = await CommandRunner.RunAsync("git", new[] { "tag", "--sort=-version:refname" }, repoDirectory,
                line => tags.Add(line), cancellationToken).ConfigureAwait(false);

            if (!result.Ok)
            {
                LastError = $"could not list tags: {Tail(result.ErrorTail)}";
                return null;
            }

            string? tag = tags.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));

            if (tag == null)
            {
                LastError = "the official osu-cc repository has no tags";
                return null;
            }

            log?.Invoke($"latest tagged build: {tag}");
            return tag.Trim();
        }

        /// <summary>Checks out the given tag and builds the runtime bundle, returning its path (or <c>null</c>).</summary>
        public async Task<string?> BuildAsync(string tag, CancellationToken cancellationToken)
        {
            if (!await checkoutTagAsync(tag, cancellationToken))
                return null;

            string bundleDirectory = Path.Combine(repoDirectory, "artifacts", "runtime");

            try
            {
                if (Directory.Exists(bundleDirectory))
                    Directory.Delete(bundleDirectory, recursive: true);
            }
            catch (IOException)
            {
                // best effort
            }

            log?.Invoke($"building runtime bundle of {tag} (this may take a while)...");

            CommandResult result = await CommandRunner.RunAsync("dotnet", new[] { "build", "osucc.build.proj", "-t:PackBootstrapBundle", "-c", "Release", "--nologo" },
                repoDirectory, line => log?.Invoke(line), cancellationToken).ConfigureAwait(false);

            if (!result.Ok)
            {
                LastError = $"build failed: {Tail(result.ErrorTail)}";
                return null;
            }

            string? bundle = Directory.Exists(bundleDirectory)
                ? Directory.GetFiles(bundleDirectory, "osucc-runtime-*.zip").FirstOrDefault()
                : null;

            if (bundle == null)
            {
                LastError = "the build produced no runtime bundle";
                return null;
            }

            return bundle;
        }

        /// <summary>Clones the official repo, or fetches the latest tags into an official-origin clone.</summary>
        private async Task<bool> ensureOfficialRepoAsync(CancellationToken cancellationToken)
        {
            bool exists = Directory.Exists(repoDirectory) && Directory.Exists(Path.Combine(repoDirectory, ".git"));

            if (exists)
            {
                var origin = new List<string>();

                CommandResult originResult = await CommandRunner.RunAsync("git", new[] { "remote", "get-url", "origin" }, repoDirectory,
                    line => origin.Add(line), cancellationToken).ConfigureAwait(false);

                string? originUrl = origin.LastOrDefault()?.Trim();

                if (originResult.Ok && !string.IsNullOrEmpty(originUrl) && normaliseUrl(originUrl) != normaliseUrl(officialUrl))
                {
                    LastError = $"local repo at {repoDirectory} points to a different remote ({originUrl}); refusing to touch it";
                    return false;
                }

                CommandResult fetchResult = await CommandRunner.RunAsync("git", new[] { "fetch", "--tags", "--force", "origin" }, repoDirectory,
                    line => log?.Invoke(line), cancellationToken).ConfigureAwait(false);

                if (!fetchResult.Ok)
                {
                    LastError = $"could not fetch the official repo: {Tail(fetchResult.ErrorTail)}";
                    return false;
                }

                return true;
            }

            // Remove a stale partial clone before retrying.
            try
            {
                if (Directory.Exists(repoDirectory))
                    Directory.Delete(repoDirectory, recursive: true);
            }
            catch (IOException)
            {
            }

            Directory.CreateDirectory(repoDirectory);

            log?.Invoke("cloning the official osu-cc repository...");

            CommandResult cloneResult = await CommandRunner.RunAsync("git", new[] { "clone", "--quiet", "--no-single-branch", officialUrl, repoDirectory },
                repoDirectory, line => log?.Invoke(line), cancellationToken).ConfigureAwait(false);

            if (!cloneResult.Ok)
            {
                LastError = $"could not clone the official repo: {Tail(cloneResult.ErrorTail)} (is git installed and on PATH?)";
                return false;
            }

            return true;
        }

        private async Task<bool> checkoutTagAsync(string tag, CancellationToken cancellationToken)
        {
            CommandResult result = await CommandRunner.RunAsync("git", new[] { "checkout", "--quiet", "--detach", tag }, repoDirectory,
                line => log?.Invoke(line), cancellationToken).ConfigureAwait(false);

            if (!result.Ok)
            {
                LastError = $"could not checkout {tag}: {Tail(result.ErrorTail)}";
                return false;
            }

            return true;
        }

        private static string normaliseUrl(string url)
            => url.Trim().TrimEnd('/').Replace("git@github.com:", "https://github.com/", StringComparison.Ordinal)
                     .Replace("ssh://git@github.com/", "https://github.com/", StringComparison.Ordinal);

        private static string Tail(string? value, int lines = 12)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string[] split = value.Split('\n');

            return split.Length <= lines
                ? value
                : string.Join('\n', split[^lines..]);
        }
    }
}
