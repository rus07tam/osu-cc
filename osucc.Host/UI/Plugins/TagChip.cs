using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK.Graphics;

namespace osucc.UI.Plugins
{
    /// <summary>
    /// A fully rounded pill chip representing one plugin tag: a surface one step darker than the
    /// card behind it with a visible outline, filling with the overlay accent on hover. Clicking it
    /// reports the tag back through <see cref="OnSelected"/> (e.g. to seed the plugins-overlay search
    /// box); without a handler it renders as an inert, non-reactive pill.
    /// </summary>
    public partial class TagChip : ClickableContainer
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly Box background;
        private readonly Box hoverFlash;
        private readonly CircularContainer surface;

        public string Tag { get; }

        public Action<string>? OnSelected { get; set; }

        public TagChip(string tag, float fontSize = 12)
        {
            Tag = tag;

            var text = new OsuSpriteText
            {
                Text = tag,
                Font = OsuFont.Default.With(size: fontSize),
                Colour = Color4.White,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            };

            AutoSizeAxes = Axes.Both;
            Action = () => OnSelected?.Invoke(tag);

            InternalChildren = new Drawable[]
            {
                surface = new CircularContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    BorderThickness = 1,
                    Children = new Drawable[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                        hoverFlash = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0,
                        },
                    },
                },
                new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Horizontal = 12, Vertical = 3 },
                    Child = text,
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            background.Colour = colourProvider.Background3;
            hoverFlash.Colour = colourProvider.Colour0;
            surface.BorderColour = colourProvider.Foreground1.Opacity(0.25f);
        }

        protected override bool OnHover(HoverEvent e)
        {
            if (OnSelected == null)
                return false;

            base.OnHover(e);
            hoverFlash.FadeTo(1, 100);
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);
            hoverFlash.FadeTo(0, 100);
        }
    }
}
