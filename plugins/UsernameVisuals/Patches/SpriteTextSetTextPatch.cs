using HarmonyLib;
using osu.Framework.Graphics.Sprites;
using osucc.Core;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Re-applies the own-username display whenever the game itself writes the text of a
    /// <see cref="UsernameVisualsText"/> (e.g. the toolbar button's scheduled <c>userChanged</c>
    /// overwriting a replaced name with the real one). The postfix sits on the non-virtual
    /// <c>SpriteText.Text</c> setter, so every such write is observed regardless of the caller;
    /// <see cref="UsernameVisualsText.ReapplyDisplay"/> is a no-op when the value is unchanged.
    /// </summary>
    internal static class SpriteTextSetTextPatch
    {
        public static bool Install(Harmony harmony)
        {
            var setText = typeof(SpriteText).GetMethod("set_Text", BindingFlags.Instance | BindingFlags.Public);
            if (setText == null)
                return false;

            harmony.Patch(setText, postfix: Reflection.HarmonyMethod(typeof(SpriteTextSetTextPatch), nameof(Postfix)));
            return true;
        }

        private static void Postfix(SpriteText __instance)
        {
            if (__instance is UsernameVisualsText gradient)
                gradient.ReapplyDisplay();
        }
    }
}
