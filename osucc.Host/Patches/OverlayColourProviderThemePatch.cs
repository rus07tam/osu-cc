using HarmonyLib;
using osucc.Core;
using osuTK.Graphics;
using System;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Re-themes the whole client's chrome by remapping the colour ramp of the game's
    /// <c>osu.Game.Overlays.OverlayColourProvider</c>. Every overlay (song select, mod select,
    /// settings, profile, chat, ...) derives its backgrounds, accents and highlights from the
    /// private <c>getColour(float saturation, float lightness)</c> method — both chrome shades
    /// (low saturation) and accent shades (full saturation) funnel through it — so intercepting
    /// that single point re-colours all of them at once.
    ///
    /// Because the transform reads <see cref="OsuCcThemeManager.Active"/> at call time, it applies
    /// to every provider instance regardless of when it is constructed.
    /// </summary>
    public static class OverlayColourProviderThemePatch
    {
        public static bool Install()
        {
            var method = Reflection.GetMethod("osu.Game.Overlays.OverlayColourProvider", "getColour", m =>
                m.GetParameters().Length == 2
                && m.GetParameters()[0].ParameterType == typeof(float)
                && m.GetParameters()[1].ParameterType == typeof(float)
                && m.ReturnType == typeof(Color4));

            if (method == null)
            {
                TimingLog.Error("OverlayColourProviderThemePatch: getColour not found");
                return false;
            }

            var prefix = Reflection.HarmonyMethod(typeof(OverlayColourProviderThemePatch), nameof(Prefix));
            HookDependencies.Main.Patch(method, prefix: prefix);
            TimingLog.Info("OverlayColourProviderThemePatch: getColour patched");
            return true;
        }

        /// <summary>Returns <c>true</c> to run the original, <c>false</c> (with <c>__result</c> set) to override.</summary>
        private static bool Prefix(float saturation, float lightness, ref Color4 __result)
        {
            if (OsuCcThemeManager.IsVanillaActive)
                return true;

            __result = OsuCcThemeManager.MapChrome(saturation, lightness);
            return false;
        }
    }
}
