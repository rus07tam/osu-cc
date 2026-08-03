using HarmonyLib;
using osu.Game.Rulesets.Mods;
using osu.Game.Utils;
using osucc.Client;
using osucc.Core;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Prevents incompatible mods from being stripped before gameplay. Targets the static
    /// <c>ModUtils.CheckValidForGameplay</c>, the choke point used by <c>OsuGame.modsChanged</c>
    /// and the ruleset-change conversion. When the flag is on, the prefix reports the set as valid.
    /// </summary>
    public static class ModUtilsGameplayPatch
    {
        public static bool Install()
        {
            var method = Reflection.GetGameType("osu.Game.Utils.ModUtils")?.GetMethod("CheckValidForGameplay", BindingFlags.Public | BindingFlags.Static);

            if (method == null)
            {
                TimingLog.Error("ModUtilsGameplayPatch: CheckValidForGameplay method not found");
                return false;
            }

            HookDependencies.Main.Patch(method, prefix: Reflection.HarmonyMethod(typeof(ModUtilsGameplayPatch), nameof(Prefix)));
            TimingLog.Info("ModUtils.CheckValidForGameplay patched (prefix)");
            return true;
        }

        private static bool Prefix(ref bool __result, ref List<Mod>? invalidMods)
        {
            if (!ClientMods.AllowIncompatibleMods)
                return true;

            __result = true;
            invalidMods = null;
            return false;
        }
    }
}
