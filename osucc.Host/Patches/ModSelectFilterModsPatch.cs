using HarmonyLib;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Mods;
using osucc.Client;
using osucc.Core;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Makes ModType.System mods selectable. Targets the private <c>ModSelectOverlay.filterMods()</c>,
    /// which hardcodes <c>ValidForSelection = mod.Type != ModType.System &amp;&amp; ...</c>. When the
    /// flag is on, the postfix re-applies the standard visibility rule to System mods so they show
    /// up in the (dynamically added) System column.
    /// </summary>
    public static class ModSelectFilterModsPatch
    {
        public static bool Install()
        {
            var method = Reflection.GetGameType("osu.Game.Overlays.Mods.ModSelectOverlay")?.GetMethod("filterMods", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (method == null)
            {
                TimingLog.Error("ModSelectFilterModsPatch: filterMods method not found");
                return false;
            }

            HookDependencies.Main.Patch(method, postfix: Reflection.HarmonyMethod(typeof(ModSelectFilterModsPatch), nameof(Postfix)));
            TimingLog.Info("ModSelectOverlay.filterMods patched (postfix)");
            return true;
        }

        private static void Postfix(ModSelectOverlay __instance)
        {
            if (!ClientMods.ShowSystemMods)
                return;

            foreach (var modState in __instance.AllAvailableMods)
            {
                if (modState.Mod.Type == ModType.System)
                    modState.ValidForSelection.Value = modState.Mod.HasImplementation && __instance.IsValidMod.Invoke(modState.Mod);
            }
        }
    }
}
