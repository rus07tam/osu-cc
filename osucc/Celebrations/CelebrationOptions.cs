using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osuTK.Graphics;

namespace osucc.Celebrations
{
    /// <summary>
    /// Customisation options for a <see cref="Celebration"/>. All values have defaults, so a
    /// celebration can be shown with just a title.
    /// </summary>
    public class CelebrationOptions
    {
        /// <summary>The large header text (e.g. "NEW BEST SCORE!"), displayed uppercased.</summary>
        public LocalisableString TitleText { get; set; } = string.Empty;

        /// <summary>Optional secondary line below the header.</summary>
        public LocalisableString? SubtitleText { get; set; }

        /// <summary>Accent colour of the glow particles.</summary>
        public Color4 AccentColour { get; set; } = Color4Extensions.FromHex("66ccff");

        /// <summary>Colour of the subtitle text.</summary>
        public Color4 SubtitleColour { get; set; } = Color4Extensions.FromHex("99eeff");

        /// <summary>Alpha of the full-screen black background dim (0 = none, 1 = opaque).</summary>
        public float BackgroundDim { get; set; } = 0.7f;

        /// <summary>Opacity of the white flash applied to the background on show.</summary>
        public float BackgroundFlashOpacity { get; set; } = 0.15f;

        /// <summary>Font size of the header text.</summary>
        public float TitleFontSize { get; set; } = 32;

        /// <summary>Font weight of the header text.</summary>
        public FontWeight TitleWeight { get; set; } = FontWeight.Light;

        /// <summary>Font size of the subtitle text.</summary>
        public float SubtitleFontSize { get; set; } = 20;

        /// <summary>Font weight of the subtitle text.</summary>
        public FontWeight SubtitleWeight { get; set; } = FontWeight.Bold;

        /// <summary>Duration (ms) of the overall fade-in and each element's entrance.</summary>
        public double FadeDuration { get; set; } = 400;

        /// <summary>Duration (ms) of a single particle's flight before it expires.</summary>
        public double ParticleDuration { get; set; } = 500;

        /// <summary>Duration (ms) after which the celebration auto-dismisses.</summary>
        public double TotalDuration { get; set; } = 4600;

        /// <summary>How many particles are spawned per frame.</summary>
        public int ParticlesPerFrame { get; set; } = 1;

        /// <summary>Whether clicking anywhere dismisses the celebration early.</summary>
        public bool DismissOnClick { get; set; } = true;
    }
}
