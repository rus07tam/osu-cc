using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osu.Game.Screens.Select;
using osucc.Core;
using osuTK;

namespace osucc.UI.SongSelect
{
    /// <summary>
    /// Pink pulsing outline with drifting particles drawn over a favourited beatmap panel in the
    /// song select carousel. Added as the front-most child of <see cref="Panel.TopLevelContent"/>,
    /// which is already masked with the panel's corner radius, so both the border and the particles
    /// are clipped to the rounded panel. Takes no input: events bubble up to the panel.
    /// </summary>
    public partial class FavouriteHighlightDrawable : CompositeDrawable
    {
        public const string HighlightName = "osucc-favourite-highlight";

        private readonly Container border;
        private readonly Container particles;

        private double lastParticleSpawnTime;

        public FavouriteHighlightDrawable()
        {
            Name = HighlightName;
            RelativeSizeAxes = Axes.Both;
            Depth = float.MinValue;

            InternalChildren = new Drawable[]
            {
                // Drawn after the particles (lower depth = front-most).
                border = new Container
                {
                    Depth = float.MinValue,
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = Panel.CORNER_RADIUS,
                    BorderThickness = 3f,
                    BorderColour = OsuCcColours.Pink,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true,
                    },
                },
                particles = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            border.Alpha = 0.9f;
            border.FadeTo(1f, 900, Easing.InOutSine)
                  .Then()
                  .FadeTo(0.85f, 900, Easing.InOutSine)
                  .Loop();
        }

        protected override void Update()
        {
            base.Update();

            if (Time.Current - lastParticleSpawnTime < 500)
                return;

            lastParticleSpawnTime = Time.Current;

            for (int i = 0; i < 2; i++)
                spawnParticle();
        }

        private void spawnParticle()
        {
            float size = RNG.NextSingle(2f, 5f);
            var colour = OsuCcColours.Pink.Opacity(RNG.NextSingle(0.5f, 1f));

            var start = new Vector2(RNG.NextSingle(0.05f, 0.95f), RNG.NextSingle(0.05f, 0.95f));
            var drift = new Vector2(RNG.NextSingle(-0.05f, 0.05f), RNG.NextSingle(0.1f, 0.3f));

            var particle = new Circle
            {
                Size = new Vector2(size),
                Colour = colour,
                Anchor = Anchor.TopLeft,
                Origin = Anchor.Centre,
                RelativePositionAxes = Axes.Both,
                Position = start,
                Alpha = 0,
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Glow,
                    Colour = colour.Opacity(0.5f),
                    Radius = 4,
                },
            };

            particles.Add(particle);

            particle.FadeIn(250, Easing.Out)
                    .Then()
                    .MoveTo(start + drift, 1400, Easing.InOutSine)
                    .FadeOut(900, Easing.In)
                    .Expire();
        }
    }
}
