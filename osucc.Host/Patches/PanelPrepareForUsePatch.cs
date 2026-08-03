using HarmonyLib;
using osu.Game.Screens.Select;
using osucc.Client;
using osucc.Core;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Adds/removes the pink favourite highlight on song select panels. Targets the base
    /// <c>Panel.PrepareForUse()</c>; every concrete panel (<c>PanelBeatmap</c>, <c>PanelBeatmapSet</c>,
    /// <c>PanelBeatmapStandalone</c>) overrides it but calls <c>base.PrepareForUse()</c>, so the
    /// postfix runs for all of them, once per panel activation (pool reuse included) and always after
    /// the panel is loaded with an up-to-date <c>Item</c>.
    /// </summary>
    public static class PanelPrepareForUsePatch
    {
        public static bool Install()
        {
            var method = Reflection.GetGameType("osu.Game.Screens.Select.Panel")
                                   ?.GetMethod("PrepareForUse", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (method == null)
            {
                TimingLog.Error("PanelPrepareForUsePatch: Panel.PrepareForUse not found");
                return false;
            }

            HookDependencies.Create("dev.osucc.favourites").Patch(method, postfix: Reflection.HarmonyMethod(typeof(PanelPrepareForUsePatch), nameof(Postfix)));
            TimingLog.Info("Panel.PrepareForUse patched (postfix)");
            return true;
        }

        private static void Postfix(Panel __instance)
        {
            try
            {
                ClientFavourites.ApplyHighlight(__instance);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PanelPrepareForUsePatch.Postfix: {ex}");
            }
        }
    }
}
