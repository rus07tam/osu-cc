using osu.Framework.Graphics.Containers;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays.Profile.Sections.Beatmaps;
using osucc.Client;
using osucc.Core;
using osucc.UI.Profile;
using System.Linq;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Adds the "download all favourites" button to the Beatmaps → Favourites section of user
    /// profiles. Targets <c>PaginatedBeatmapContainer.load()</c>; the base
    /// <c>ProfileSubsection.load()</c> runs before it (osu.Framework invokes background dependency
    /// loaders base-first), so the content flow (<c>Children[1]</c>) already exists at postfix time.
    /// Sections are rebuilt on every profile fetch, so the button is always fresh.
    /// </summary>
    public static class PaginatedBeatmapContainerLoadPatch
    {
        public static bool Install()
        {
            var method = Reflection.GetGameType("osu.Game.Overlays.Profile.Sections.Beatmaps.PaginatedBeatmapContainer")
                                   ?.GetMethod("load", BindingFlags.Instance | BindingFlags.NonPublic);

            if (method == null)
            {
                TimingLog.Error("PaginatedBeatmapContainerLoadPatch: PaginatedBeatmapContainer.load not found");
                return false;
            }

            HookDependencies.Main.Patch(method, postfix: Reflection.HarmonyMethod(typeof(PaginatedBeatmapContainerLoadPatch), nameof(Postfix)));
            TimingLog.Info("PaginatedBeatmapContainer.load patched (postfix)");
            return true;
        }

        private static void Postfix(PaginatedBeatmapContainer __instance)
        {
            try
            {
                var typeField = Reflection.FindField(__instance.GetType(), "type");
                if (typeField?.GetValue(__instance) is not BeatmapSetType type || type != BeatmapSetType.Favourite)
                    return;

                // Children[0] = header, Children[1] = the content FillFlowContainer from CreateContent().
                if (__instance.Children.Count <= 1 || __instance.Children[1] is not FillFlowContainer flow)
                {
                    TimingLog.Info("PaginatedBeatmapContainerLoadPatch: content flow not found");
                    return;
                }

                if (flow.Children.OfType<DownloadAllFavouritesButton>().Any())
                    return;

                var button = new DownloadAllFavouritesButton(__instance);
                flow.Add(button);
                flow.SetLayoutPosition(button, -100f);
                TimingLog.Info("DownloadAllFavouritesButton added to Favourites section");
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PaginatedBeatmapContainerLoadPatch.Postfix: {ex}");
            }
        }
    }
}
