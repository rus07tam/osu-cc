using osu.Game.Rulesets.Mods;
using osucc.Client;
using osucc.Core;
using System.Collections.Generic;

namespace osucc.Patches
{
    /// <summary>
    /// Prevents incompatible mods from being stripped before gameplay. Targets the static
    /// <c>ModUtils.CheckValidForGameplay</c>.
    /// </summary>
    public sealed class ModUtilsGameplayPatch : OsuCcPatch
    {
        public ModUtilsGameplayPatch()
            : base("osu.Game.Utils.ModUtils", "CheckValidForGameplay", MethodType.Prefix)
        {
        }

        public override bool Condition => ClientMods.AllowIncompatibleMods;

        public static bool Prefix(ref bool __result, ref List<Mod>? invalidMods)
        {
            __result = true;
            invalidMods = null;
            return false;
        }
    }
}
