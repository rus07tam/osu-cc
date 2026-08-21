using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osucc.UI.Plugins
{
    /// <summary>
    /// A static pill showing one plugin tag: a raised surface one step lighter than the card behind
    /// it with a soft shadow. Pills are not clickable (searching is done through the search box);
    /// <see cref="more"/> renders a recessive, dimmer "+N" counter.
    /// </summary>
    public partial class TagChip : Container
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly Box background;
        private readonly OsuSpriteText text;

        private readonly bool more;

        public string Tag { get; }

        public TagChip(string tag, float fontSize = 12, bool more = false)
        {
            Tag = tag;
            this.more = more;

            AutoSizeAxes = Axes.Both;

            text = new OsuSpriteText
            {
                Text = tag,
                Font = OsuFont.Default.With(size: fontSize, weight: FontWeight.SemiBold),
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
            };

            Child = new Container
            {
                AutoSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 11,
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Shadow,
                    Colour = Color4.Black.Opacity(0.12f),
                    Radius = 3,
                    Offset = new Vector2(0, 1),
                },
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Horizontal = 10,
                            Vertical = 4,
                        },
                        Child = text,
                    },
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (more)
            {
                background.Colour = colourProvider.Background5;
                text.Colour = Color4.White.Opacity(0.5f);
            }
            else
            {
                background.Colour = colourProvider.Background2;
                text.Colour = Color4.White.Opacity(0.95f);
            }
        }
    }
}
