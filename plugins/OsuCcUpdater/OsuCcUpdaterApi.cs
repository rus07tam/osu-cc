using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osucc.Client;
using osucc.Common;
using osucc.Plugin;
using System.Globalization;

namespace OsuCcUpdater
{
    /// <summary>
    /// The updater's public API and coordinator. Resolves the osu-cc data root through the shared
    /// resolver (the same one the launcher uses), compares the installed hook against the latest
    /// osu-cc release, downloads the runtime bundle from GitHub releases or builds it locally from
    /// the official repo, and stages the result into <c>&lt;osu-cc&gt;/staging</c> with an
    /// <see cref="UpdateMarker"/>. Live files are never touched (they are locked while the game
    /// runs); the launcher applies the staging folder on the next launch.
    /// </summary>
    public sealed class OsuCcUpdaterApi : IDisposable
    {
        private readonly IOsuCcPluginHost host;
        private readonly PluginSettings settings;
        private readonly HttpClient http;
        private readonly string osuCcDirectory;
        private readonly object busyLock = new();
        private bool busy;

        private readonly Bindable<UpdateSource> source = new(UpdateSource.GithubBundle);

        public OsuCcUpdaterApi(IOsuCcPluginHost host, PluginSettings settings)
        {
            this.host = host;
            this.settings = settings;

            osuCcDirectory = resolveOsuCcDirectory();

            http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("osu-cc-updater");

            // The source selector persists as a plain string; mirror it into the enum bindable.
            string persistedSource = settings.Bind("source", "GithubBundle").Value;
            source.Value = Enum.TryParse<UpdateSource>(persistedSource, ignoreCase: true, out UpdateSource parsed)
                ? parsed
                : UpdateSource.GithubBundle;

            source.BindValueChanged(e => settings.Bind("source", "GithubBundle").Value = e.NewValue.ToString(), true);

            Instance = this;
        }

        /// <summary>Convenience singleton, matching the pattern of the other plugin APIs.</summary>
        public static OsuCcUpdaterApi? Instance { get; internal set; }

        /// <summary>The installed hook version (from <c>osucc.dll</c>), or an empty string when no hook is installed.</summary>
        public string CurrentVersion => OsuCcVersionReader.Read(hookDllPath) ?? string.Empty;

        /// <summary>The newest version known from the last check, if any.</summary>
        public string? LatestVersion => settings.Get<string>("latest_version");

        /// <summary>The staged update's version, if an update is waiting for the next launch.</summary>
        public string? StagedVersion => UpdateMarker.TryRead(stagingDirectory)?.Version;

        /// <summary>True while a check/build/stage operation is running.</summary>
        public bool Busy
        {
            get
            {
                lock (busyLock)
                    return busy;
            }
        }

        /// <summary>True when a staged update is waiting for the next launch.</summary>
        public bool HasStagedUpdate => UpdateMarker.TryRead(stagingDirectory) != null;

        /// <summary>The update source selector, persisted through plugin settings.</summary>
        public Bindable<UpdateSource> Source => source;

        /// <summary>Whether to auto-check on game start (persisted).</summary>
        public Bindable<bool> AutoCheck => settings.Bind("auto_check", true);

        /// <summary>Raised whenever the busy flag or the staged/latest state may have changed.</summary>
        public event Action? StateChanged;

        private string hookDllPath => Path.Combine(osuCcDirectory, OsuCcLayout.HookDirectoryName, OsuCcLayout.HookDllName);
        private string stagingDirectory => OsuCcLayout.StagingDirectory(osuCcDirectory);

        private void notifyChanged()
        {
            try
            {
                StateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                host.Log($"state callback failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Stages the newest available build from the given source. Runs on the calling thread
        /// (never the update thread); a background task is the expected caller.
        /// </summary>
        public async Task<UpdateResult> StageUpdateAsync(UpdateSource requestedSource, CancellationToken cancellationToken = default)
        {
            lock (busyLock)
            {
                if (busy)
                    return UpdateResult.Of(UpdateOutcome.Failed, message: "the updater is already busy");

                busy = true;
            }

            try
            {
                notifyChanged();

                string? current = string.IsNullOrEmpty(CurrentVersion) ? null : CurrentVersion;

                UpdateResult result = requestedSource == UpdateSource.GithubBundle
                    ? await stageFromGithubAsync(current, cancellationToken).ConfigureAwait(false)
                    : await stageFromLocalBuildAsync(current, cancellationToken).ConfigureAwait(false);

                // Remember the newest version we saw, and when we last checked, so the UI and the
                // auto-check throttle have something to read without hitting GitHub again.
                if (!string.IsNullOrEmpty(result.Version))
                    settings.Bind("latest_version", string.Empty).Value = result.Version!;

                settings.Bind("last_check", string.Empty).Value = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

                return result;
            }
            finally
            {
                lock (busyLock)
                    busy = false;

                notifyChanged();
            }
        }

        /// <summary>
        /// Runs <see cref="StageUpdateAsync"/> and posts a toast summarising the outcome. Used by
        /// the toolbar button and the settings buttons.
        /// </summary>
        public async Task<UpdateResult> RunAndNotifyAsync(UpdateSource requestedSource, CancellationToken cancellationToken = default)
        {
            UpdateResult result = await StageUpdateAsync(requestedSource, cancellationToken).ConfigureAwait(false);

            switch (result.Outcome)
            {
                case UpdateOutcome.Staged:
                case UpdateOutcome.AlreadyStaged:
                    postNotification(OsuCcUpdaterStrings.NotifyUpdateStaged(result.Version ?? string.Empty), NotificationKind.Success);
                    break;

                case UpdateOutcome.UpToDate:
                    postNotification(OsuCcUpdaterStrings.NotifyUpToDate(result.Version ?? string.Empty), NotificationKind.Info);
                    break;

                case UpdateOutcome.Failed:
                    postNotification(OsuCcUpdaterStrings.NotifyFailed(result.Message ?? "unknown error"), NotificationKind.Error);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Auto-check on game start: runs when enabled and no check happened in the last six hours
        /// (unauthenticated GitHub has a tight rate limit), silently staging an update if one exists.
        /// </summary>
        public void AutoCheckIfDue()
        {
            if (!AutoCheck.Value)
                return;

            if (!string.IsNullOrEmpty(settings.Bind("last_check", string.Empty).Value)
                && DateTimeOffset.TryParse(settings.Bind("last_check", string.Empty).Value, out DateTimeOffset lastCheck)
                && DateTimeOffset.UtcNow - lastCheck < TimeSpan.FromHours(6))
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                UpdateResult result = await StageUpdateAsync(UpdateSource.GithubBundle).ConfigureAwait(false);

                if (result.Outcome == UpdateOutcome.Staged)
                    postNotification(OsuCcUpdaterStrings.NotifyUpdateStaged(result.Version ?? string.Empty), NotificationKind.Success);
            });
        }

        /// <summary>Downloads the runtime bundle from the latest GitHub release and stages it.</summary>
        private async Task<UpdateResult> stageFromGithubAsync(string? current, CancellationToken cancellationToken)
        {
            var github = new GithubBundleSource(http, message => host.Log(message));

            (string? tag, string? downloadUrl, string? error) = await github.QueryLatestAsync(cancellationToken).ConfigureAwait(false);

            if (tag == null || string.IsNullOrEmpty(downloadUrl))
                return UpdateResult.Of(UpdateOutcome.Failed, message: error);

            string version = normaliseVersion(tag);

            // A staged update from an earlier run that already covers this version needs no new download.
            if (HasStagedUpdate && StagedVersion != null && OsuCcVersionReader.IsAtLeast(StagedVersion, version))
                return UpdateResult.Of(UpdateOutcome.AlreadyStaged, version);

            if (current != null && OsuCcVersionReader.IsAtLeast(current, version))
                return UpdateResult.Of(UpdateOutcome.UpToDate, version);

            string? bundle = await github.DownloadAsync(downloadUrl, cancellationToken).ConfigureAwait(false);

            if (bundle == null)
                return UpdateResult.Of(UpdateOutcome.Failed, version, "failed to download the runtime bundle");

            stageBundle(bundle, version, "github");
            return UpdateResult.Of(UpdateOutcome.Staged, version);
        }

        /// <summary>Clones/updates the official repo, builds the runtime bundle locally and stages it.</summary>
        private async Task<UpdateResult> stageFromLocalBuildAsync(string? current, CancellationToken cancellationToken)
        {
            var local = new LocalBuildSource(osuCcDirectory, message => host.Log(message));

            string? tag = await local.ResolveLatestTagAsync(cancellationToken).ConfigureAwait(false);

            if (tag == null)
                return UpdateResult.Of(UpdateOutcome.Failed, message: local.LastError);

            string version = normaliseVersion(tag);

            if (HasStagedUpdate && StagedVersion != null && OsuCcVersionReader.IsAtLeast(StagedVersion, version))
                return UpdateResult.Of(UpdateOutcome.AlreadyStaged, version);

            if (current != null && OsuCcVersionReader.IsAtLeast(current, version))
                return UpdateResult.Of(UpdateOutcome.UpToDate, version);

            string? bundle = await local.BuildAsync(tag, cancellationToken).ConfigureAwait(false);

            if (bundle == null)
                return UpdateResult.Of(UpdateOutcome.Failed, version, local.LastError);

            stageBundle(bundle, version, "build");
            return UpdateResult.Of(UpdateOutcome.Staged, version);
        }

        /// <summary>Replaces the staging folder with the bundle's payload and writes the marker.</summary>
        private void stageBundle(string bundleFile, string version, string source)
        {
            StagingWriter.FromBundle(bundleFile, osuCcDirectory);
            UpdateMarker.Write(stagingDirectory, new UpdateMarker(version, source, DateTimeOffset.UtcNow.ToString("O")));

            try
            {
                File.Delete(bundleFile);
            }
            catch (IOException)
            {
                // best effort; the bundle is a temp file
            }
        }

        /// <summary>Strips a leading <c>v</c> from a tag so versions persist and display consistently (v1.0.0 → 1.0.0).</summary>
        private static string normaliseVersion(string tag)
            => tag.StartsWith('v') || tag.StartsWith('V') ? tag[1..] : tag;

        private void postNotification(LocalisableString text, NotificationKind kind)
        {
            if (host.Scheduler != null)
                host.Scheduler.Add(() => host.Notify(text, kind));
            else
                host.Notify(text, kind);
        }

        public void Dispose()
        {
            StateChanged = null;
            http.Dispose();
            Instance = null;
        }

        private static string resolveOsuCcDirectory()
        {
            // The hook dll always lives in <osu-cc>/hook; the shared resolver mirrors how the
            // launcher finds the same root, so staging lands where the launcher applies it.
            string hookDirectory = Path.GetDirectoryName(typeof(OsuCcPlugin).Assembly.Location) ?? string.Empty;
            return OsuCcDataRootResolver.Resolve(hookDirectory);
        }
    }
}
