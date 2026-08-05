using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osucc.Localisation;
using osuTK.Graphics;

namespace osucc.Celebrations
{
    /// <summary>A "new personal best" celebration: header + achieved score subtitle, blue particles.</summary>
    public partial class BestScoreCelebration : Celebration
    {
        private static readonly Color4 particleColour = Color4Extensions.FromHex("66ccff"); // == OsuColour.Blue
        private static readonly Color4 subtitleColour = Color4Extensions.FromHex("99eeff"); // == OsuColour.BlueLight

        public BestScoreCelebration(LocalisableString titleText, long totalScore)
            : base(new CelebrationOptions
            {
                TitleText = titleText,
                SubtitleText = PersonalBestStrings.Score(totalScore),
                AccentColour = particleColour,
                SubtitleColour = subtitleColour,
                BackgroundDim = 0.7f,
            })
        {
        }
    }
}
