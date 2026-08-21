using osu.Framework.Graphics.Containers;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays.Profile.Sections.Beatmaps;
using osucc.Core;
using osucc.UI.Profile;
using System.Linq;

namespace osucc.Patches
{
    /// <summary>
    /// Adds the "download all favourites" button to the Beatmaps → Favourites section of user profiles.
    /// Targets <c>PaginatedBeatmapContainer.load()</c>.
    /// </summary>
    public sealed class PaginatedBeatmapContainerLoadPatch : OsuCcPatch
    {
        public PaginatedBeatmapContainerLoadPatch()
            : base("osu.Game.Overlays.Profile.Sections.Beatmaps.PaginatedBeatmapContainer", "load", MethodType.Postfix)
        {
        }

        public void Postfix(PaginatedBeatmapContainer __instance)
        {
            var typeField = Reflection.FindField(__instance.GetType(), "type");
            if (typeField?.GetValue(__instance) is not BeatmapSetType type || type != BeatmapSetType.Favourite)
                return;

            // Children[0] = header, Children[1] = the content FillFlowContainer from CreateContent().
            if (__instance.Children.Count <= 1 || __instance.Children[1] is not FillFlowContainer flow)
            {
                LogInfo("content flow not found");
                return;
            }

            if (flow.Children.OfType<DownloadAllFavouritesButton>().Any())
                return;

            var button = new DownloadAllFavouritesButton(__instance);
            flow.Add(button);
            flow.SetLayoutPosition(button, -100f);
            LogInfo("DownloadAllFavouritesButton added to Favourites section");
        }
    }
}
