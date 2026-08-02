using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osuTK;
using osuTK.Graphics;

namespace osucc.Client
{
    /// <summary>
    /// Notification variant that shows a FontAwesome icon or a texture (such as a plugin's folder
    /// icon) in place of the generic kind icon. FontAwesome icons are tinted by the kind colour;
    /// textures show naturally. Supports an optional bold title line (such as the plugin name).
    /// </summary>
    public partial class PluginNotification : Notification
    {
        private LocalisableString text;
        private LocalisableString title;

        public override LocalisableString Text
        {
            get => text;
            set
            {
                text = value;
                TextFlow.Text = text;
            }
        }

        /// <summary>Optional bold title line rendered above the message body.</summary>
        public LocalisableString Title
        {
            get => title;
            set
            {
                title = value;
                titleText.Text = value;
                titleText.Alpha = value.ToString().Length == 0 ? 0 : 1;
            }
        }

        protected TextFlowContainer TextFlow { get; }

        private readonly OsuSpriteText titleText;

        private readonly Box iconBackground;

        public PluginNotification(IconUsage? icon, Texture? iconTexture, Color4 iconColour)
        {
            IconContent.AddRange(new Drawable[]
            {
                iconBackground = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                },
            });

            if (icon is { } usage)
            {
                IconContent.Add(new SpriteIcon
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Icon = usage,
                    Size = new Vector2(20),
                    Colour = iconColour,
                });
            }
            else if (iconTexture != null)
            {
                IconContent.Add(new Sprite
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(28),
                    FillMode = FillMode.Fit,
                    Texture = iconTexture,
                });
            }

            Content.AddRange(new Drawable[]
            {
                titleText = new OsuSpriteText
                {
                    Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Bold),
                    Alpha = 0,
                },
                TextFlow = new OsuTextFlowContainer(t => t.Font = t.Font.With(size: 14, weight: FontWeight.Medium))
                {
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    Text = text,
                },
            });
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, OverlayColourProvider colourProvider)
        {
            Light.Colour = colours.Green;
            iconBackground.Colour = colourProvider.Background5;
        }
    }
}
