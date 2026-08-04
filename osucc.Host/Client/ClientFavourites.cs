using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Game.Online.API;
using osu.Game.Screens.Select;
using osucc.Core;
using osucc.UI.SongSelect;
using System.Collections.Specialized;
using System.Linq;

namespace osucc.Client
{
    /// <summary>
    /// Live favourite tracking for the song select highlight. Keeps a snapshot of the current
    /// player's favourited beatmap set online IDs (fed by <see cref="ILocalUserState.FavouriteBeatmapSets"/>
    /// — the same source as the game's "Favourites" grouping) and adds/removes the pink
    /// <see cref="FavouriteHighlightDrawable"/> on live carousel panels.
    /// </summary>
    public static class ClientFavourites
    {
        private static readonly object lockObject = new();

        private static bool enabled;
        private static HashSet<int> favouriteIds = new HashSet<int>();
        private static readonly HashSet<Panel> panels = new HashSet<Panel>();

        private static Bindable<bool>? enabledBindable;
        private static ILocalUserState? localUserState;
        private static bool attached;

        public static void Attach(SpecialsConfigManager config)
        {
            if (attached)
                return;

            // Strong ref: ConfigManager.GetBindable returns weak copies, so the subscription
            // below would die after the first (immediate) fire otherwise.
            enabledBindable = config.GetBindable<bool>(SpecialsSetting.FavouriteMapHighlight);
            enabledBindable.BindValueChanged(e => onEnabledChanged(e.NewValue), true);

            var api = ClientApi.Game?.Dependencies?.Get(typeof(IAPIProvider)) as IAPIProvider;

            if (api != null)
            {
                localUserState = api.LocalUserState;
                localUserState.FavouriteBeatmapSets.CollectionChanged += onFavouritesChanged;
                refreshFavouriteIds();
            }
            else
            {
                TimingLog.Info("ClientFavourites: IAPIProvider not available; highlights will stay off");
            }

            attached = true;
            TimingLog.Info($"ClientFavourites attached (enabled={enabled}, favourites={favouriteIds.Count})");
        }

        /// <summary>Called from the <c>Panel.PrepareForUse</c> postfix (update thread) whenever a carousel panel is (re)activated.</summary>
        public static void ApplyHighlight(Panel panel)
        {
            registerPanel(panel);
            applyToPanel(panel);
        }

        /// <summary>Re-applies the highlight to every live panel. Called when the config toggle or the favourite set changes.</summary>
        public static void RefreshHighlights()
        {
            Panel[] live;
            lock (lockObject)
            {
                panels.RemoveWhere(DrawableHelper.IsDisposed);
                live = panels.ToArray();
            }

            TimingLog.Info($"ClientFavourites.RefreshHighlights: {live.Length} panel(s), enabled={enabled}, favourites={favouriteIds.Count}");

            foreach (var panel in live)
            {
                try
                {
                    applyToPanel(panel);
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"ClientFavourites.RefreshHighlights: {ex}");
                }
            }
        }

        private static void onEnabledChanged(bool newValue)
        {
            enabled = newValue;
            RefreshHighlights();
            TimingLog.Info($"Favourite map highlight enabled={newValue}");
        }

        private static void onFavouritesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            refreshFavouriteIds();

            // The list is updated on an API request thread; re-apply highlights on the update thread.
            var scheduler = Reflection.GetScheduler(ClientApi.Game);
            if (scheduler != null)
                scheduler.Add(RefreshHighlights);
            else
                RefreshHighlights();
        }

        private static void refreshFavouriteIds()
            => favouriteIds = localUserState?.FavouriteBeatmapSets.ToHashSet() ?? new HashSet<int>();

        private static void applyToPanel(Panel panel)
        {
            var top = panel.TopLevelContent;
            if (top == null)
                return;

            var existing = findHighlight(top);

            int onlineId = getFavouriteOnlineId(panel);
            bool show = enabled && onlineId > 0 && favouriteIds.Contains(onlineId);

            if (show && existing == null)
            {
                top.Add(new FavouriteHighlightDrawable());
                TimingLog.Info($"Favourite highlight added to {panel.GetType().Name} (id {onlineId})");
            }
            else if (!show && existing != null)
            {
                top.Remove(existing, true);
                TimingLog.Info("Favourite highlight removed");
            }
        }

        private static int getFavouriteOnlineId(Panel panel)
        {
            return panel.Item?.Model switch
            {
                GroupedBeatmap groupedBeatmap => groupedBeatmap.Beatmap.BeatmapSet?.OnlineID ?? -1,
                GroupedBeatmapSet groupedSet => groupedSet.BeatmapSet.OnlineID,
                _ => -1,
            };
        }

        private static FavouriteHighlightDrawable? findHighlight(Container top)
            => top.Children.OfType<FavouriteHighlightDrawable>().FirstOrDefault();

        private static void registerPanel(Panel panel)
        {
            lock (lockObject)
            {
                panels.RemoveWhere(DrawableHelper.IsDisposed);
                panels.Add(panel);
            }
        }
    }
}
