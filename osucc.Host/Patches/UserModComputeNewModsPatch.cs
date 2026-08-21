using osu.Game.Rulesets.Mods;
using osucc.Client;
using osucc.Core;
using System.Collections.Generic;

namespace osucc.Patches
{
    /// <summary>
    /// Lets incompatible mods survive selection. Targets
    /// <c>UserModSelectOverlay.ComputeNewModsFromSelection</c>.
    /// </summary>
    public sealed class UserModComputeNewModsPatch : OsuCcPatch
    {
        public UserModComputeNewModsPatch()
            : base("osu.Game.Overlays.Mods.UserModSelectOverlay", "ComputeNewModsFromSelection", MethodType.Prefix)
        {
        }

        public override bool Condition => ClientMods.AllowIncompatibleMods;

        public static bool Prefix(ref IReadOnlyList<Mod> __result, IReadOnlyList<Mod> newSelection)
        {
            __result = newSelection;
            return false;
        }
    }
}
