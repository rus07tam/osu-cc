using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.Profile;
using osu.Game.Overlays.Profile.Sections.Beatmaps;
using osucc.Core;
using osucc.Localisation;
using osucc.UI.Profile;
using System.Linq;

namespace osucc.Client
{
    /// <summary>
    /// "Download all favourites" support for the profile Beatmaps section. Resolves the downloader
    /// and beatmap store from DI, fetches every favourite page of the viewed user on demand and
    /// enqueues downloads for the sets that are not already present locally. Also toggles the live
    /// visibility of the buttons on the config toggle.
    /// </summary>
    public static class ClientProfileDownloads
    {
        private const int pageSize = 50;

        private static readonly object lockObject = new();
        private static readonly HashSet<DownloadAllFavouritesButton> buttons = new HashSet<DownloadAllFavouritesButton>();

        private static Bindable<bool>? enabledBindable;
        private static bool attached;

        public static void Attach(SpecialsConfigManager config)
        {
            if (attached)
                return;

            // Strong ref: ConfigManager.GetBindable returns weak copies, so the subscription
            // below would die after the first (immediate) fire otherwise.
            enabledBindable = config.GetBindable<bool>(SpecialsSetting.ProfileFavouriteDownloadButton);
            enabledBindable.BindValueChanged(e => refreshButtons(e.NewValue), true);

            attached = true;
            TimingLog.Info("ClientProfileDownloads attached");
        }

        /// <summary>Called from the <c>PaginatedBeatmapContainer.load</c> postfix when the button is created.</summary>
        public static void Register(DownloadAllFavouritesButton button)
        {
            lock (lockObject)
            {
                buttons.RemoveWhere(DrawableHelper.IsDisposed);
                buttons.Add(button);
                button.Alpha = enabledBindable?.Value == true ? 1 : 0;
            }
        }

        private static void refreshButtons(bool enabled)
        {
            DownloadAllFavouritesButton[] live;
            lock (lockObject)
            {
                buttons.RemoveWhere(DrawableHelper.IsDisposed);
                live = buttons.ToArray();
            }

            foreach (var button in live)
                button.Alpha = enabled ? 1 : 0;

            TimingLog.Info($"ClientProfileDownloads.refreshButtons: {live.Length} button(s), enabled={enabled}");
        }

        /// <summary>
        /// Fetches every favourite page for the profile's current user and enqueues downloads for the
        /// sets that are not already imported. <paramref name="onComplete"/> is invoked when the fetch
        /// chain finishes. Returns <c>false</c> (and invokes <paramref name="onComplete"/> immediately)
        /// when the request cannot be started.
        /// </summary>
        public static bool DownloadAllFavourites(PaginatedBeatmapContainer container, Action? onComplete = null)
        {
            var game = ClientApi.Game;
            var api = game?.Dependencies?.Get(typeof(IAPIProvider)) as IAPIProvider;
            var downloader = game?.Dependencies?.Get(typeof(BeatmapModelDownloader)) as BeatmapModelDownloader;

            var user = readProfileUser(container);

            if (api == null || downloader == null || user == null)
            {
                TimingLog.Info("ClientProfileDownloads: unavailable (api/downloader/user missing), nothing to do");
                onComplete?.Invoke();
                return false;
            }

            var manager = game?.Dependencies?.Get(typeof(BeatmapManager)) as BeatmapManager;
            var localIds = manager?.GetAllUsableBeatmapSets().Select(s => s.OnlineID).ToHashSet() ?? new HashSet<int>();

            TimingLog.Info($"ClientProfileDownloads: fetching favourites for user {user.Id}");
            fetchPage(api, user.Id, 0, new List<APIBeatmapSet>(), downloader, localIds, onComplete);
            return true;
        }

        private static void fetchPage(IAPIProvider api, long userId, int offset, List<APIBeatmapSet> favourites, BeatmapModelDownloader downloader, HashSet<int> localIds, Action? onComplete)
        {
            var request = new GetUserBeatmapsRequest(userId, BeatmapSetType.Favourite, new PaginationParameters(offset, pageSize));
            request.Success += items =>
            {
                favourites.AddRange(items);

                if (items.Count >= pageSize)
                {
                    fetchPage(api, userId, offset + pageSize, favourites, downloader, localIds, onComplete);
                    return;
                }

                // Enqueueing touches BeatmapModelDownloader.CurrentDownloads (a plain list) and posts
                // notifications, so run it on the update thread; the Success handler is on the API thread.
                var scheduler = Reflection.GetScheduler(ClientApi.Game);
                if (scheduler != null)
                    scheduler.Add(() =>
                    {
                        enqueueDownloads(favourites, downloader, localIds);
                        onComplete?.Invoke();
                    });
                else
                {
                    enqueueDownloads(favourites, downloader, localIds);
                    onComplete?.Invoke();
                }
            };

            request.Failure += _ =>
            {
                var scheduler = Reflection.GetScheduler(ClientApi.Game);
                if (scheduler != null)
                    scheduler.Add(() => onComplete?.Invoke());
                else
                    onComplete?.Invoke();
            };
            api.Queue(request);
        }

        private static void enqueueDownloads(List<APIBeatmapSet> favourites, BeatmapModelDownloader downloader, HashSet<int> localIds)
        {
            var toDownload = favourites.Where(s => s.OnlineID > 0 && !localIds.Contains(s.OnlineID)).ToArray();
            int skipped = favourites.Count - toDownload.Length;
            int enqueued = toDownload.Count(s => downloader.Download(s));

            TimingLog.Info($"ClientProfileDownloads: enqueued {enqueued}/{favourites.Count} favourites, skipped {skipped} already present");

            ClientNotifications.Info(enqueued == 0
                ? DownloadStrings.NoNewFavourites(favourites.Count, skipped)
                : skipped > 0
                    ? DownloadStrings.DownloadingSkipped(enqueued, skipped)
                    : DownloadStrings.Downloading(enqueued));
        }

        private static APIUser? readProfileUser(PaginatedBeatmapContainer container)
        {
            var field = Reflection.FindField(container.GetType(), "User");
            if (field?.GetValue(container) is not Bindable<UserProfileData?> userBindable)
                return null;

            return userBindable.Value?.User;
        }
    }
}
