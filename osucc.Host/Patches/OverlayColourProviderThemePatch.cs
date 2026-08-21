using osucc.Core;
using osuTK.Graphics;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Re-themes the whole client's chrome by remapping the colour ramp of the game's
    /// <c>osu.Game.Overlays.OverlayColourProvider</c>.
    /// </summary>
    public sealed class OverlayColourProviderThemePatch : OsuCcPatch
    {
        public OverlayColourProviderThemePatch()
            : base("osu.Game.Overlays.OverlayColourProvider", "getColour", m =>
                m.GetParameters().Length == 2
                && m.GetParameters()[0].ParameterType == typeof(float)
                && m.GetParameters()[1].ParameterType == typeof(float)
                && m.ReturnType == typeof(Color4), MethodType.Prefix)
        {
        }

        public override bool Condition => !OsuCcThemeManager.IsVanillaActive;

        public static bool Prefix(float saturation, float lightness, ref Color4 __result)
        {
            __result = OsuCcThemeManager.MapChrome(saturation, lightness);
            return false;
        }
    }
}
