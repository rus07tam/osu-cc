using HarmonyLib;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Mods;
using osucc.Client;
using osucc.Core;
using osucc.UI.Mods;
using System.Collections.Generic;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Appends the <see cref="RandomModsButton"/> to the mod-select footer.
    /// Targets <c>ModSelectFooterContent.CreateButtons()</c> (protected virtual). The postfix
    /// also runs for <c>FreeModSelectFooterContent</c>, which calls <c>base.CreateButtons()</c>
    /// explicitly, so the button is added there too. Reads the private <c>overlay</c> field from
    /// the declaring base type, which works for both footer variants.
    /// </summary>
    public static class ModSelectFooterCreateButtonsPatch
    {
        public static bool Install()
        {
            var method = Reflection.GetGameType("osu.Game.Overlays.Mods.ModSelectFooterContent")?.GetMethod("CreateButtons", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                TimingLog.Error("ModSelectFooterCreateButtonsPatch: CreateButtons method not found");
                return false;
            }

            HookDependencies.Create("dev.osucc.mods.footer").Patch(method, postfix: Reflection.HarmonyMethod(typeof(ModSelectFooterCreateButtonsPatch), nameof(Postfix)));
            TimingLog.Info("ModSelectFooterContent.CreateButtons patched (postfix)");
            return true;
        }

        private static void Postfix(ModSelectFooterContent __instance, ref IEnumerable<ShearedButton> __result)
        {
            try
            {
                if (!ClientMods.RandomModsButton)
                    return;

                var overlay = readOverlay(__instance);
                if (overlay == null)
                {
                    TimingLog.Error("ModSelectFooterCreateButtonsPatch.Postfix: overlay field not found");
                    return;
                }

                __result = __result.Append(new RandomModsButton(overlay));
            }
            catch (Exception ex)
            {
                TimingLog.Error($"ModSelectFooterCreateButtonsPatch.Postfix: {ex}");
            }
        }

        // The overlay field is private to the base ModSelectFooterContent and hidden in
        // FreeModSelectFooterContent; reading it from the declaring type is valid for both.
        private static ModSelectOverlay? readOverlay(ModSelectFooterContent instance)
        {
            var field = typeof(ModSelectFooterContent).GetField("overlay", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return field?.GetValue(instance) as ModSelectOverlay;
        }
    }
}
