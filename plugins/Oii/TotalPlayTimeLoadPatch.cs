using HarmonyLib;
using osu.Framework.Graphics.Containers;
using osu.Game.Overlays.Profile.Header.Components;
using osucc.Client;
using osucc.Core;

namespace Oii
{
    /// <summary>
    /// Inserts an <see cref="OiiIndicator"/> into the profile header's main details flow right after
    /// the play time display, binding it to the same user data so it follows user/ruleset changes.
    /// </summary>
    internal static class TotalPlayTimeLoadPatch
    {
        public static bool Install(Harmony harmony)
            => PatchHelper.AttachPostfix(harmony, "osu.Game.Overlays.Profile.Header.Components.TotalPlayTime", "load", typeof(TotalPlayTimeLoadPatch), nameof(Postfix));

        private static void Postfix(TotalPlayTime __instance)
        {
            // Parent is only assigned after load() returns (CompositeDrawable.loadChild), so the
            // indicator is inserted on the first update frame, once the whole tree is loaded.
            var scheduler = Reflection.GetScheduler(ClientApi.Game);
            scheduler?.AddOnce(() => insertIndicator(__instance));
        }

        private static void insertIndicator(TotalPlayTime instance)
        {
            if (instance.Parent is not FillFlowContainer flow)
                return;

            var indicator = new OiiIndicator();
            indicator.User.BindTo(instance.User);
            flow.Insert((int)flow.GetLayoutPosition(instance) + 1, indicator);

            TimingLog.Info("Oii indicator inserted");
        }
    }
}
