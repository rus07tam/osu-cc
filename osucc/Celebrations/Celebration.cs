using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osucc.Celebrations
{
    /// <summary>
    /// Full-screen celebration overlay, modelled after the game's <c>MedalAnimation</c>:
    /// dimmed background, particles bursting from the centre, large header and optional
    /// subtitle. Appearance is driven by <see cref="CelebrationOptions"/>; particle visuals
    /// can be overridden via <see cref="CreateParticle"/>. Self-contained (no
    /// <c>[Resolved]</c> deps), so it can be added anywhere in the drawable tree.
    /// </summary>
    public partial class Celebration : VisibilityContainer
    {
        private readonly CelebrationOptions options;

        private readonly Container content;
        private readonly Container particleContainer;
        private readonly Box background;
        private readonly OsuSpriteText title;
        private readonly OsuSpriteText subtitle;

        /// <summary>Creates a celebration from the given options.</summary>
        public Celebration(CelebrationOptions options)
        {
            this.options = options;

            RelativeSizeAxes = Axes.Both;

            Child = content = new Container
            {
                Alpha = 0,
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black.Opacity(options.BackgroundDim),
                    },
                    particleContainer = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0f,
                    },
                    title = new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = new CaseTransformableString(options.TitleText, Casing.UpperCase),
                        Font = OsuFont.GetFont(size: options.TitleFontSize, weight: options.TitleWeight),
                        Alpha = 0f,
                        Scale = new Vector2(1f / 0.76f),
                    },
                    subtitle = new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.TopCentre,
                        Text = options.SubtitleText ?? string.Empty,
                        Font = OsuFont.GetFont(size: options.SubtitleFontSize, weight: options.SubtitleWeight),
                        Colour = options.SubtitleColour,
                        Alpha = 0f,
                        Margin = new MarginPadding { Top = 60 },
                        Scale = new Vector2(1f / 0.6f),
                    },
                }
            };

            if (options.DismissOnClick)
            {
                // Full-screen hit area above the visuals, so a click dismisses the celebration.
                AddInternal(new ClickableContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Action = Dismiss,
                });
            }

            Show();
        }

        protected override void PopIn()
        {
            this.FadeIn(options.FadeDuration, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            this.FadeOut(options.FadeDuration * 0.5, Easing.OutQuint);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            content.FadeIn(options.FadeDuration, Easing.OutQuint);
            background.FlashColour(Color4.White.Opacity(options.BackgroundFlashOpacity), options.FadeDuration);

            particleContainer.FadeIn(options.FadeDuration, Easing.OutQuint);

            using (BeginDelayedSequence(options.FadeDuration))
            {
                title.FadeIn(options.FadeDuration, Easing.OutQuint)
                     .ScaleTo(1f, options.FadeDuration * 2, Easing.OutElastic);

                using (BeginDelayedSequence(options.FadeDuration * 0.5))
                {
                    if (!LocalisableString.IsNullOrEmpty(options.SubtitleText))
                        subtitle.FadeIn(options.FadeDuration, Easing.OutQuint)
                                .ScaleTo(1f, options.FadeDuration * 2, Easing.OutElastic);
                }
            }

            Scheduler.AddDelayed(() =>
            {
                if (IsAlive)
                    Dismiss();
            }, options.TotalDuration);
        }

        protected override void Update()
        {
            base.Update();

            for (int i = 0; i < options.ParticlesPerFrame; i++)
                particleContainer.Add(CreateParticle());
        }

        /// <summary>Creates a single burst particle. Override to change its visuals.</summary>
        protected virtual Drawable CreateParticle() => new CelebrationParticle(RNG.Next(0, 359), options.AccentColour, options.ParticleDuration);

        /// <summary>Fades out and removes this celebration.</summary>
        public void Dismiss()
        {
            if (!IsAlive)
                return;

            FinishTransforms(true);
            Hide();
            Expire();
        }

        /// <summary>A radially-outward-flying glow circle, cloned from <c>MedalAnimation.MedalParticle</c>.</summary>
        private sealed partial class CelebrationParticle : CircularContainer
        {
            private readonly float direction;
            private readonly Color4 colour;
            private readonly double duration;

            private Vector2 positionForOffset(float offset) => new Vector2((float)(offset * Math.Sin(direction)), (float)(offset * Math.Cos(direction)));

            public CelebrationParticle(float direction, Color4 colour, double duration)
            {
                this.direction = direction;
                this.colour = colour;
                this.duration = duration;
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                Position = positionForOffset(200);
                Masking = true;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Glow,
                    Colour = colour.Opacity(0.5f),
                    Radius = 5,
                };

                this.MoveTo(positionForOffset(200 + 200), duration);
                this.FadeOut(duration);
                Expire();
            }
        }
    }
}
