using osu.Game.Screens.Select;
using osucc.Client;
using osucc.Core;

namespace osucc.Patches
{
    /// <summary>
    /// Adds/removes the pink favourite highlight on song select panels. Targets the base
    /// <c>Panel.PrepareForUse()</c>.
    /// </summary>
    public sealed class PanelPrepareForUsePatch : OsuCcPatch
    {
        public PanelPrepareForUsePatch()
            : base("osu.Game.Screens.Select.Panel", "PrepareForUse", MethodType.Postfix)
        {
        }

        public static void Postfix(Panel __instance)
        {
            ClientFavourites.ApplyHighlight(__instance);
        }
    }
}
