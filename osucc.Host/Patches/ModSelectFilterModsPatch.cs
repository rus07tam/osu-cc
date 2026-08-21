using osu.Game.Overlays.Mods;
using osu.Game.Rulesets.Mods;
using osucc.Client;
using osucc.Core;

namespace osucc.Patches
{
    /// <summary>
    /// Makes ModType.System mods selectable. Targets the private <c>ModSelectOverlay.filterMods()</c>.
    /// </summary>
    public sealed class ModSelectFilterModsPatch : OsuCcPatch
    {
        public ModSelectFilterModsPatch()
            : base("osu.Game.Overlays.Mods.ModSelectOverlay", "filterMods", MethodType.Postfix)
        {
        }

        public override bool Condition => ClientMods.ShowSystemMods;

        public static void Postfix(ModSelectOverlay __instance)
        {
            foreach (var modState in __instance.AllAvailableMods)
            {
                if (modState.Mod.Type == ModType.System)
                    modState.ValidForSelection.Value = modState.Mod.HasImplementation && __instance.IsValidMod.Invoke(modState.Mod);
            }
        }
    }
}
