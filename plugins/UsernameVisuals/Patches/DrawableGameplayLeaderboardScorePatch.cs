using HarmonyLib;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;
using osu.Game.Screens.Play.HUD;
using osucc.Core;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Replaces the in-game HUD leaderboard's username text with a gradient copy after
    /// <c>load()</c> builds the panel. The <c>usernameText</c> field is rewritten so later colour
    /// updates (friend / has-quit highlighting) keep targeting the visible gradient text.
    /// </summary>
    internal static class DrawableGameplayLeaderboardScorePatch
    {
        private static readonly FieldInfo? usernameTextField = Reflection.GetField("osu.Game.Screens.Play.HUD.DrawableGameplayLeaderboardScore", "usernameText");

        public static bool Install(Harmony harmony)
            => PatchHelper.AttachPostfix(harmony, "osu.Game.Screens.Play.HUD.DrawableGameplayLeaderboardScore", "load", typeof(DrawableGameplayLeaderboardScorePatch), nameof(Postfix));

        private static void Postfix(DrawableGameplayLeaderboardScore __instance)
        {
            if (usernameTextField?.GetValue(__instance) is not OsuSpriteText current || current is UsernameVisualsText)
                return;

            var gradient = UsernameVisualsText.CopyOf(current);
            gradient.User = __instance.User;

            var grid = DrawableHelper.FindGridContaining(__instance, current);

            if (grid != null)
                DrawableHelper.SwapInGrid(grid, current, gradient);
            else
                DrawableHelper.SwapInParent(current, gradient);

            usernameTextField!.SetValue(__instance, gradient);
        }
    }
}
