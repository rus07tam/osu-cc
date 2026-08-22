using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;
using System.Linq;

namespace osucc.UI.Overlays
{
    /// <summary>
    /// Base class for osu!cc's sheared full-screen overlays: dimmed main content, canonical
    /// background, a sheared header with title/description/close and a scrollable main area —
    /// the same shared lifetime semantics as <see cref="OsuCcWaveOverlay"/> (mutual exclusion,
    /// depth, dim, close/back/restore), only with the sheared visual style.
    /// </summary>
    public abstract partial class OsuCcShearedOverlay : OsuCcOverlayBase
    {
        /// <summary>The sheared header (title/description/close). Typed for direct member access.</summary>
        protected new OsuCcShearedOverlayHeader Header => (OsuCcShearedOverlayHeader)base.Header;

        protected override float HeaderHeight => ShearedOverlayHeader.HEIGHT;

        private Box? backdrop;

        protected OsuCcShearedOverlay(OverlayColourScheme colourScheme)
            : base(colourScheme)
        {
        }

        protected override Drawable CreateBackdrop() => backdrop = new Box
        {
            RelativeSizeAxes = Axes.Both,
            Colour = ColourProvider.Background6.Opacity(0.75f),
        };

        protected override Drawable CreateHeader() => new OsuCcShearedOverlayHeader { Close = Hide };

        public override void ChangeColourScheme(OverlayColourScheme scheme)
        {
            base.ChangeColourScheme(scheme);
            UpdateColours();
        }

        protected virtual void UpdateColours()
        {
            if (backdrop != null)
                backdrop.Colour = ColourProvider.Background6.Opacity(0.75f);

            if (base.Header is OsuCcShearedOverlayHeader header)
                header.UpdateColours(ColourProvider);
        }

        protected override void OnOverlayShown() => Header.MoveToY(0, FadeInDuration, Easing.OutQuint);

        protected override void OnOverlayHidden() => Header.MoveToY(-Header.DrawHeight, FadeOutDuration, Easing.OutQuint);

        public partial class OsuCcShearedOverlayHeader : ShearedOverlayHeader
        {
            private readonly Container iconContainer;
            private IconUsage icon;

            public IconUsage HeaderIcon
            {
                get => icon;
                set
                {
                    icon = value;
                    updateIcon();
                }
            }

            public IconUsage Icon
            {
                get => icon;
                set
                {
                    icon = value;
                    updateIcon();
                }
            }

            public LocalisableString TitleText
            {
                set => Title = value;
            }

            public LocalisableString DescriptionText
            {
                set => Description = value;
            }

            private const float corner_radius = 14;

            public OsuCcShearedOverlayHeader()
            {
                iconContainer = new Container
                {
                    Size = new Vector2(38),
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreRight,
                    X = 88,
                    Margin = new MarginPadding
                    {
                        Top = corner_radius,
                    },
                    Alpha = 0,
                };

                if (InternalChild is Container root && root.Children.Count >= 2 && root.Children[1] is Container content)
                {
                    content.Add(iconContainer);
                }
            }

            private void updateIcon()
            {
                if (icon.Equals(default) || icon.Icon == 0)
                {
                    iconContainer.Clear();
                    iconContainer.Alpha = 0;
                    return;
                }

                iconContainer.Alpha = 1;
                iconContainer.Child = new SpriteIcon
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    FillMode = FillMode.Fit,
                    Icon = icon,
                };
            }

            public void UpdateColours(OverlayColourProvider colourProvider)
            {
                if (InternalChild is Container root && root.Children.Count >= 2)
                {
                    if (root.Children[0] is Container underlay)
                    {
                        underlay.BorderColour = osu.Framework.Graphics.Colour.ColourInfo.GradientVertical(Colour4.Black, colourProvider.Dark4);
                        if (underlay.Children.OfType<Box>().FirstOrDefault() is Box underlayBox)
                            underlayBox.Colour = colourProvider.Dark4;
                    }

                    if (root.Children[1] is Container content)
                    {
                        content.BorderColour = osu.Framework.Graphics.Colour.ColourInfo.GradientVertical(colourProvider.Dark3, colourProvider.Dark1);
                        if (content.Children.OfType<Box>().FirstOrDefault() is Box contentBox)
                            contentBox.Colour = colourProvider.Dark3;

                        if (content.Children.OfType<IconButton>().FirstOrDefault() is IconButton closeBtn)
                            closeBtn.IconHoverColour = colourProvider.Highlight1;
                    }
                }
            }
        }
    }
}
