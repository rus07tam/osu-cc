using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;
using osu.Game.Screens.Select;
using osucc.Core;

namespace UsernameVisuals
{
    /// <summary>
    /// Replaces the song-select leaderboard's username <see cref="TruncatingSpriteText"/> with a
    /// gradient copy after the panel's <c>load()</c> builds it. The truncation behaviour is
    /// preserved by enabling <see cref="SpriteText.Truncate"/> directly (bypassing the throwing
    /// <c>OsuSpriteText</c> hide).
    /// </summary>
    [OsuCcPatch("osu.Game.Screens.Select.BeatmapLeaderboardScore", "load")]
    internal static class BeatmapLeaderboardScorePatch
    {

        private static void Postfix(BeatmapLeaderboardScore __instance)
        {
            string username = __instance.Score.User.Username;

            var text = DrawableHelper.FindInTree(__instance, d => d is TruncatingSpriteText sprite && sprite.Text.ToString() == username) as TruncatingSpriteText;

            if (text == null)
                return;

            var gradient = UsernameVisualsText.CopyOf(text);
            gradient.User = __instance.Score.User;

            DrawableHelper.SwapInParent(text, gradient);
        }
    }
}
