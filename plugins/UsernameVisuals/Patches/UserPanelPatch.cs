using HarmonyLib;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;
using osu.Game.Users;
using osucc.Core;

namespace UsernameVisuals
{
    /// <summary>
    /// Replaces the username sprite created by <c>UserPanel.CreateUsername()</c> with a gradient
    /// text carrying the panel's user. Covers every panel that builds its name through the
    /// shared <c>CreateUsername()</c> (rank, list, grid and brick panels).
    /// </summary>
    internal static class UserPanelPatch
    {
        public static bool Install(Harmony harmony)
            => PatchHelper.AttachPostfix(harmony, "osu.Game.Users.UserPanel", "CreateUsername", typeof(UserPanelPatch), nameof(Postfix));

        private static void Postfix(UserPanel __instance, ref OsuSpriteText __result)
        {
            if (__result == null || __result is UsernameVisualsText)
                return;

            var gradient = UsernameVisualsText.CopyOf(__result);
            gradient.User = __instance.User;
            __result = gradient;
        }
    }
}
