using osu.Game.Graphics.Sprites;
using osu.Game.Screens.Play.HUD;
using osucc.Core;
using osucc.Plugin;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Replaces the in-game HUD leaderboard's username text with a gradient copy after
    /// <c>load()</c> builds the panel.
    /// </summary>
    public sealed class DrawableGameplayLeaderboardScorePatch : PluginPatch<UsernameVisualsPlugin>
    {
        private static readonly FieldInfo? usernameTextField = Reflection.GetField("osu.Game.Screens.Play.HUD.DrawableGameplayLeaderboardScore", "usernameText");

        public DrawableGameplayLeaderboardScorePatch(UsernameVisualsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Screens.Play.HUD.DrawableGameplayLeaderboardScore", "load", MethodType.Postfix)
        {
        }

        public static void Postfix(DrawableGameplayLeaderboardScore __instance)
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
