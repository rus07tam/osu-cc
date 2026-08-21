using osu.Game.Overlays.Mods;
using osucc.Client;
using osucc.Core;

namespace osucc.Patches
{
    /// <summary>
    /// Tracks live mod-select overlays so the Specials toggles react immediately. Targets
    /// <c>ModSelectOverlay.LoadComplete()</c>.
    /// </summary>
    public sealed class ModSelectLoadCompletePatch : OsuCcPatch
    {
        public ModSelectLoadCompletePatch()
            : base("osu.Game.Overlays.Mods.ModSelectOverlay", "LoadComplete", MethodType.Postfix)
        {
        }

        public static void Postfix(ModSelectOverlay __instance)
        {
            ClientMods.Register(__instance);
        }
    }
}
