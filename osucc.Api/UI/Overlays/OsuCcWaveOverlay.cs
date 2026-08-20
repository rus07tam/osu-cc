using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osuTK.Graphics;

namespace osucc.UI.Overlays
{
    /// <summary>
    /// Full-screen overlay in the "wave" style of the game's online overlays (beatmap listing,
    /// changelog, wiki): coloured wave bands sweep over the dimmed background as the page
    /// (a stock-style header with icon/title/description and optional tabs, plus the scrollable
    /// main area) fades in on top. On close the waves sweep back down while the page fades out.
    /// Same lifetime semantics as <see cref="OsuCcShearedOverlay"/> (mutual exclusion, depth, dim,
    /// back/close, restore).
    /// </summary>
    public abstract partial class OsuCcWaveOverlay : OsuCcOverlayBase
    {
        /// <summary>The stock-style header (icon/title/description + content row). Typed for direct member access.</summary>
        protected new OsuCcOverlayHeader Header => (OsuCcOverlayHeader)base.Header;

        // WaveContainer plays its own PopIn/PopOut samples ("UI/wave-pop-in" and "UI/overlay-big-pop-out").
        protected override string PopInSampleName => string.Empty;
        protected override string PopOutSampleName => string.Empty;

        /// <summary>Quick overlay container fade-in while WaveContainer handles the 800ms slide & wave sweep.</summary>
        protected override double PopInFadeDuration => 100;

        /// <summary>Matches the <see cref="WaveContainer"/> disappear duration (500ms).</summary>
        protected override double PopOutFadeDuration => WaveContainer.DISAPPEAR_DURATION;

        private WaveContainer? waves;
        private Box? backdrop;

        protected override float HeaderHeight => 0;

        protected OsuCcWaveOverlay(OverlayColourScheme colourScheme)
            : base(colourScheme)
        {
            Width = 0.85f;
            Anchor = Anchor.TopCentre;
            Origin = Anchor.TopCentre;
            Masking = true;
            EdgeEffect = new EdgeEffectParameters
            {
                Colour = Color4.Black.Opacity(0),
                Type = EdgeEffectType.Shadow,
                Hollow = true,
                Radius = 10,
            };
        }

        protected override Drawable CreateBackdrop() => backdrop = new Box
        {
            RelativeSizeAxes = Axes.Both,
            Colour = ColourProvider.Background6.Opacity(0.9f),
        };

        public override void ChangeColourScheme(OverlayColourScheme scheme)
        {
            base.ChangeColourScheme(scheme);
            UpdateColours();
        }

        protected virtual void UpdateColours()
        {
            if (waves != null)
            {
                waves.FirstWaveColour = ColourProvider.Light4;
                waves.SecondWaveColour = ColourProvider.Light3;
                waves.ThirdWaveColour = ColourProvider.Dark4;
                waves.FourthWaveColour = ColourProvider.Dark3;
            }

            if (backdrop != null)
                backdrop.Colour = ColourProvider.Background6.Opacity(0.9f);

            if (base.Header is OsuCcOverlayHeader header)
                header.UpdateColours(ColourProvider);
        }

        protected override Drawable ComposeContent(Drawable backdrop)
        {
            MainAreaContent.RelativeSizeAxes = Axes.X;
            MainAreaContent.AutoSizeAxes = Axes.Y;
            MainAreaContent.Padding = new MarginPadding
            {
                Horizontal = WaveOverlayContainer.HORIZONTAL_PADDING,
                Bottom = Padding,
            };

            waves = new WaveContainer
            {
                RelativeSizeAxes = Axes.Both,
                FirstWaveColour = ColourProvider.Light4,
                SecondWaveColour = ColourProvider.Light3,
                ThirdWaveColour = ColourProvider.Dark4,
                FourthWaveColour = ColourProvider.Dark3,
                Children = new Drawable[]
                {
                    backdrop,
                    new OverlayScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        ScrollbarVisible = false,
                        Child = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Children = new Drawable[]
                            {
                                Header,
                                MainAreaContent,
                            },
                        },
                    },
                }
            };

            return waves;
        }

        protected override Drawable CreateHeader() => new OsuCcOverlayHeader();

        protected override void OnOverlayShown()
        {
            waves?.Show();
            FadeEdgeEffectTo(WaveContainer.SHADOW_OPACITY, WaveContainer.APPEAR_DURATION, Easing.Out);
        }

        protected override void OnOverlayHidden()
        {
            waves?.Hide();
            FadeEdgeEffectTo(0, WaveContainer.DISAPPEAR_DURATION, Easing.In);
        }
    }
}
