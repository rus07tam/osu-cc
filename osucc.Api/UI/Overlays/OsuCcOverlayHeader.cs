using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using System.Linq;

namespace osucc.UI.Overlays
{
    /// <summary>
    /// Full-width header for full-screen overlays in the style of the game's online overlays
    /// (beatmap listing, changelog, wiki): a <c>Dark5</c> title bar with an icon, a title and a
    /// description (<see cref="OsuCcOverlayTitle"/>), followed by a content row for tabs/filters.
    /// Scrolls away together with the page content when used from <see cref="OsuCcWaveOverlay"/>.
    /// No visible close button: the owning overlay is closed via back / click outside.
    /// </summary>
    public partial class OsuCcOverlayHeader : OverlayHeader
    {
        /// <summary>The strongly-typed title (icon + title + description).</summary>
        public new OsuCcOverlayTitle Title => (OsuCcOverlayTitle)base.Title;

        /// <summary>Row directly under the title bar where the owning overlay adds tabs or filters.</summary>
        public Container ContentRow { get; private set; } = null!;

        private Box contentBackground = null!;

        public LocalisableString TitleText
        {
            set => Title.Title = value;
        }

        public LocalisableString DescriptionText
        {
            set => Title.Description = value;
        }

        public IconUsage HeaderIcon
        {
            set => Title.Icon = value;
        }

        protected override OverlayTitle CreateTitle() => new OsuCcOverlayTitle();

        protected override Drawable CreateContent() => new Container
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Children = new Drawable[]
            {
                contentBackground = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                },
                ContentRow = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding
                    {
                        Horizontal = WaveOverlayContainer.HORIZONTAL_PADDING,
                        Vertical = 10,
                    },
                },
            }
        };

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            UpdateColours(colourProvider);
        }

        public void UpdateColours(OverlayColourProvider colourProvider)
        {
            if (contentBackground != null)
                contentBackground.Colour = colourProvider.Dark4;

            // titleBackground lives inside HeaderInfo -> Container -> Box
            if (HeaderInfo != null)
            {
                foreach (var container in HeaderInfo.Children.OfType<Container>())
                {
                    foreach (var box in container.Children.OfType<Box>())
                        box.Colour = colourProvider.Dark5;
                }
            }

            // Update accent colour of any tab controls in ContentRow
            if (ContentRow != null)
            {
                foreach (var tabControl in ContentRow.Children.OfType<IHasAccentColour>())
                    tabControl.AccentColour = colourProvider.Highlight1;
            }
        }

        public partial class OsuCcOverlayTitle : OverlayTitle
        {
            public new LocalisableString Title
            {
                set => base.Title = value;
            }

            public new LocalisableString Description
            {
                set => base.Description = value;
            }

            public new IconUsage Icon
            {
                set => base.Icon = value;
            }
        }
    }
}
