using osu.Framework.Graphics.Containers;
using osu.Game.Overlays;
using osucc.Core;
using osucc.Plugin;
using osucc.UI.Overlays;

namespace osucc.Patches
{
    public sealed class OsuGameOverlayPatch : OsuCcPatch
    {
        public OsuGameOverlayPatch()
            : base("osu.Game.OsuGame", "showOverlayAboveOthers", MethodType.Postfix)
        {
        }

        public static void Postfix()
        {
            OsuCcOverlayBase.HideAll();
        }
    }
}
