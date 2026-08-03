using HarmonyLib;
using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Mods;
using osucc.Client;
using osucc.Core;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Lets incompatible mods survive selection. Targets
    /// <c>UserModSelectOverlay.ComputeNewModsFromSelection</c>, which strips newly selected mods
    /// that clash with the existing set. When the Specials "allow incompatible mods" flag is on,
    /// the prefix short-circuits and keeps the new selection unchanged.
    /// </summary>
    public static class UserModComputeNewModsPatch
    {
        public static bool Install()
        {
            var method = Reflection.GetGameType("osu.Game.Overlays.Mods.UserModSelectOverlay")?.GetMethod("ComputeNewModsFromSelection", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                TimingLog.Error("UserModComputeNewModsPatch: ComputeNewModsFromSelection method not found");
                return false;
            }

            HookDependencies.Main.Patch(method, prefix: Reflection.HarmonyMethod(typeof(UserModComputeNewModsPatch), nameof(Prefix)));
            TimingLog.Info("UserModSelectOverlay.ComputeNewModsFromSelection patched (prefix)");
            return true;
        }

        private static bool Prefix(ref IReadOnlyList<Mod> __result, IReadOnlyList<Mod> newSelection)
        {
            if (!ClientMods.AllowIncompatibleMods)
                return true;

            __result = newSelection;
            return false;
        }
    }
}
