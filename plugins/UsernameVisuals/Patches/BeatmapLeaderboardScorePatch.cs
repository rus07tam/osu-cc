using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;
using osu.Game.Screens.Select;
using osucc.Core;
using osucc.Plugin;

namespace UsernameVisuals
{
    /// <summary>
    /// Replaces the song-select leaderboard's username <see cref="TruncatingSpriteText"/> with a gradient copy.
    /// </summary>
    public sealed class BeatmapLeaderboardScorePatch : PluginPatch<UsernameVisualsPlugin>
    {
        public BeatmapLeaderboardScorePatch(UsernameVisualsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Screens.Select.BeatmapLeaderboardScore", "load", MethodType.Postfix)
        {
        }

        public static void Postfix(BeatmapLeaderboardScore __instance)
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
