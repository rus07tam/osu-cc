using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;

namespace osucc.UI.Overlays
{
    /// <summary>
    /// Base class for osu!cc's sheared full-screen overlays: dimmed background, sheared header
    /// with title/description/close, scrollable main area. Not based on
    /// <see cref="osu.Game.Overlays.Mods.ShearedOverlayContainer"/>: that registers itself as the
    /// footer's active overlay in <c>PopIn()</c>, and <see cref="osu.Game.Screens.Footer.ScreenFooter"/>
    /// only allows a single active footer overlay, so opening two such overlays in a row throws
    /// "Cannot set overlay content while one is already present". Deriving from
    /// <see cref="OsuFocusedOverlayContainer"/> (like the game's toolbar overlays) keeps us in the
    /// plain <c>overlayContent</c> layer, so multiple osu!cc overlays open independently.
    /// </summary>
    public abstract partial class OsuCcShearedOverlay : OsuFocusedOverlayContainer
    {
        public new const float Padding = 14;

        private const double fadeInDuration = 400;
        private const double fadeOutDuration = 500;

        [Cached]
        public OverlayColourProvider ColourProvider { get; }

        /// <summary>The sheared header (title/description/close).</summary>
        protected ShearedOverlayHeader Header { get; private set; } = null!;

        /// <summary>Content displayed below the header.</summary>
        protected Container MainAreaContent { get; private set; } = null!;

        protected override bool StartHidden => true;

        protected override bool BlockNonPositionalInput => true;

        // The dim comes from the local background box; the game's screen-wide dim would also
        // affect the layer we are placed on.
        protected override bool DimMainContent => false;

        protected OsuCcShearedOverlay(OverlayColourScheme colourScheme)
        {
            RelativeSizeAxes = Axes.Both;

            ColourProvider = new OverlayColourProvider(colourScheme);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourProvider.Background6.Opacity(0.75f),
                    },
                    Header = new ShearedOverlayHeader
                    {
                        Anchor = Anchor.TopCentre,
                        Depth = float.MinValue,
                        Origin = Anchor.TopCentre,
                        Close = Hide,
                    },
                    MainAreaContent = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Top = ShearedOverlayHeader.HEIGHT,
                            Bottom = Padding,
                        },
                    },
                }
            };
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (State.Value == Visibility.Visible)
            {
                Hide();
                return true;
            }

            return base.OnClick(e);
        }

        protected override void PopIn()
        {
            this.FadeIn(fadeInDuration, Easing.OutQuint);
            Header.MoveToY(0, fadeInDuration, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            base.PopOut();
            this.FadeOut(fadeOutDuration, Easing.OutQuint);
            Header.MoveToY(-Header.DrawHeight, fadeOutDuration, Easing.OutQuint);
        }
    }
}
