using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Text;
using osu.Framework.Utils;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Users;
using osuTK;
using osuTK.Graphics;
using System.Collections;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// An <see cref="OsuSpriteText"/> rendered with a horizontal gradient palette. The gradient
    /// always wins over tint colours: each glyph is painted with the palette sampled by its
    /// horizontal position, at full alpha, multiplied only by the drawable's overall alpha
    /// (hover fades, dimming, etc.). With no palette resolved it falls back to the normal
    /// single-colour rendering of <see cref="OsuSpriteText"/>.
    /// </summary>
    public partial class UsernameVisualsText : OsuSpriteText
    {
        private IUser? user;
        private Color4[] palette = Array.Empty<Color4>();
        private IBindable<APIUser>? localUserBindable;
        private bool replacing;
        private bool hide;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        /// <summary>
        /// The user whose palette this text resolves. Setting it (re)applies the palette and the
        /// own-display mode via <see cref="UsernameVisualsResolver"/>; <c>null</c> renders as
        /// normal text.
        /// </summary>
        public IUser? User
        {
            get => user;
            set
            {
                user = value;
                applyState();
            }
        }

        /// <summary>The currently effective palette; empty when rendering normally.</summary>
        public IReadOnlyList<Color4> Palette => palette;

        /// <summary>
        /// Whether <see cref="User"/> should follow <see cref="IAPIProvider.LocalUser"/> as it
        /// loads/changes. Set for surfaces that always display the local user (the toolbar
        /// button), whose swap may happen before the local user is available.
        /// </summary>
        public bool TrackLocalUser { get; set; }

        public UsernameVisualsText()
        {
            UsernameVisualsResolver.Changed += onResolverChanged;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            localUserBindable = api.LocalUser.GetBoundCopy();
            localUserBindable.BindValueChanged(localUser =>
            {
                if (TrackLocalUser)
                    User = localUser.NewValue;
                onResolverChanged();
            });
            applyState();
        }

        protected override void Dispose(bool isDisposing)
        {
            localUserBindable?.UnbindAll();
            UsernameVisualsResolver.Changed -= onResolverChanged;
            base.Dispose(isDisposing);
        }

        /// <summary>
        /// Creates a gradient text carrying the visual properties of an existing sprite text.
        /// Colour and size are intentionally not copied; the gradient wins when a palette is
        /// resolved, and leaving the size untouched keeps <see cref="SpriteText"/>'s internal
        /// autosizing enabled (copying it would freeze the template's pre-load (0,0) snapshot).
        /// </summary>
        public static UsernameVisualsText CopyOf(OsuSpriteText template)
        {
            var result = new UsernameVisualsText
            {
                Text = template.Text,
                Font = template.Font,
                Shadow = template.Shadow,
                ShadowColour = template.ShadowColour,
                ShadowOffset = template.ShadowOffset,
                Anchor = template.Anchor,
                Origin = template.Origin,
                RelativeSizeAxes = template.RelativeSizeAxes,
                Padding = template.Padding,
                Margin = template.Margin,
                MaxWidth = template.MaxWidth,
                Shear = template.Shear,
            };

            ((SpriteText)result).Truncate = ((SpriteText)template).Truncate;
            ((SpriteText)result).EllipsisString = ((SpriteText)template).EllipsisString;
            return result;
        }

        private void onResolverChanged()
        {
            Scheduler.AddOnce(applyState);
        }

        /// <summary>
        /// Re-evaluates the palette and own-display mode. Called on the update thread; exposed so
        /// external patches (such as chat username writes) can re-apply the display without waiting
        /// for a resolver event.
        /// </summary>
        public void ReapplyDisplay() => Scheduler.AddOnce(applyState);

        private void applyState()
        {
            var localUser = api?.LocalUser.Value;
            var resolved = UsernameVisualsResolver.Resolve(user, localUser);

            palette = (Color4[]?)resolved ?? Array.Empty<Color4>();

            applyOwnDisplay(UsernameVisualsResolver.OwnModeFor(user, localUser));

            // Tracked texts always show the local user, so also repair a stale/empty name when
            // the game's own write missed our swapped instance (e.g. a renamed field).
            if (TrackLocalUser && !replacing && user != null
                && !string.Equals(Text.ToString(), user.Username, StringComparison.Ordinal))
            {
                Text = user.Username;
            }

            Invalidate(Invalidation.DrawInfo);
        }

        private void applyOwnDisplay(UsernameVisualsResolver.OwnNameMode mode)
        {
            hide = mode == UsernameVisualsResolver.OwnNameMode.Hide;

            // The writes are guarded so re-applying after the game overwrote the text (or after
            // our own replace write) never churns an identical value: SpriteText.Text is not
            // virtual, so a set_Text postfix re-enters here and the guard stops the loop.
            if (mode == UsernameVisualsResolver.OwnNameMode.Replace)
            {
                if (!replacing || !string.Equals(Text.ToString(), UsernameVisualsResolver.ReplaceName, StringComparison.Ordinal))
                    Text = UsernameVisualsResolver.ReplaceName;
                replacing = true;
            }
            else if (replacing)
            {
                string realName = user?.Username ?? Text.ToString() ?? string.Empty;
                if (!string.Equals(Text.ToString(), realName, StringComparison.Ordinal))
                    Text = realName;
                replacing = false;
            }
        }

        protected override DrawNode CreateDrawNode() => new UsernameVisualsDrawNode(this);

        private sealed partial class UsernameVisualsDrawNode : TexturedShaderDrawNode
        {
            // SpriteText.characters is a private property, resolved once and cached.
            private static readonly Lazy<PropertyInfo?> charactersProperty = new(() =>
                typeof(SpriteText).GetProperty("characters", BindingFlags.Instance | BindingFlags.NonPublic));

            private static readonly Lazy<PropertyInfo?> shadowOffsetProperty = new(() =>
                typeof(SpriteText).GetProperty("premultipliedShadowOffset", BindingFlags.Instance | BindingFlags.NonPublic));

            private static readonly Lazy<PropertyInfo?> glyphTextureProperty = new(() =>
                typeof(TextBuilderGlyph).GetProperty(nameof(TextBuilderGlyph.Texture)));

            private static readonly Lazy<PropertyInfo?> glyphRectProperty = new(() =>
                typeof(TextBuilderGlyph).GetProperty(nameof(TextBuilderGlyph.DrawRectangle)));

            private readonly List<GlyphPart> parts = new();
            private Color4[] palette = Array.Empty<Color4>();
            private bool shadow;
            private Vector2 shadowOffset;
            private bool hide;
            private Quad hideQuad;

            private UsernameVisualsText GradientSource => (UsernameVisualsText)base.Source;

            public UsernameVisualsDrawNode(UsernameVisualsText source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                buildParts();
                buildHideQuad();
                palette = GradientSource.palette;
                shadow = GradientSource.Shadow;
                hide = GradientSource.hide;

                if (shadow)
                    shadowOffset = shadowOffsetProperty.Value?.GetValue(GradientSource) is Vector2 offset ? offset : Vector2.Zero;
            }

            protected override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                BindTextureShader(renderer);

                if (hide)
                {
                    renderer.DrawQuad(renderer.WhitePixel, hideQuad, ColourInfo.SingleColour(Color4.White));
                }
                else
                {
                    if (shadow)
                        drawShadow(renderer);

                    for (int i = 0; i < parts.Count; i++)
                        renderer.DrawQuad(parts[i].Texture, parts[i].DrawQuad, colourFor(parts[i].Progress), inflationPercentage: parts[i].InflationPercentage);
                }

                UnbindTextureShader(renderer);
            }

            private void drawShadow(IRenderer renderer)
            {
                // Replicate SpriteText's shadow: a single shadow colour derived from the average
                // colour (palette average when gradient is active), with brightness-based falloff.
                var average = palette.Length == 0 ? (Color4)DrawColourInfo.Colour.AverageColour : averagePalette();
                float shadowAlpha = MathF.Pow(Math.Max(Math.Max(average.R, average.G), average.B), 2);

                ColourInfo shadowColour = DrawColourInfo.Colour;
                shadowColour.ApplyChild(GradientSource.ShadowColour.Opacity(GradientSource.ShadowColour.A * shadowAlpha));

                for (int i = 0; i < parts.Count; i++)
                {
                    var part = parts[i];
                    var shadowQuad = new Quad(
                        part.DrawQuad.TopLeft + shadowOffset,
                        part.DrawQuad.TopRight + shadowOffset,
                        part.DrawQuad.BottomLeft + shadowOffset,
                        part.DrawQuad.BottomRight + shadowOffset);

                    renderer.DrawQuad(part.Texture, shadowQuad, shadowColour, inflationPercentage: part.InflationPercentage);
                }
            }

            private ColourInfo colourFor(float progress)
            {
                if (palette.Length == 0)
                    return DrawColourInfo.Colour;

                Color4 colour = samplePalette(progress);
                colour.A = DrawColourInfo.Colour.AverageColour.Alpha;
                return colour;
            }

            private Color4 samplePalette(float progress)
            {
                if (palette.Length == 1)
                    return palette[0];

                float scaled = Math.Clamp(progress, 0f, 1f) * (palette.Length - 1);
                int index = (int)MathF.Floor(scaled);

                if (index >= palette.Length - 1)
                    return palette[^1];

                float remainder = scaled - index;
                return Interpolation.ValueAt(remainder, palette[index], palette[index + 1], 0, 1);
            }

            private Color4 averagePalette()
            {
                float r = 0, g = 0, b = 0;

                foreach (Color4 colour in palette)
                {
                    r += colour.R;
                    g += colour.G;
                    b += colour.B;
                }

                return new Color4(r / palette.Length, g / palette.Length, b / palette.Length, 1);
            }

            private void buildParts()
            {
                parts.Clear();

                var charactersInfo = charactersProperty.Value;
                var textureProperty = glyphTextureProperty.Value;
                var rectProperty = glyphRectProperty.Value;

                if (charactersInfo == null || textureProperty == null || rectProperty == null)
                    return;

                if (charactersInfo.GetValue(GradientSource) is not IList characters || characters.Count == 0)
                    return;

                Vector2 inflationAmount = DrawInfo.MatrixInverse.ExtractScale().Xy;

                float minX = float.MaxValue;
                float maxX = float.MinValue;

                for (int i = 0; i < characters.Count; i++)
                {
                    object? glyph = characters[i];
                    var rect = (RectangleF)rectProperty.GetValue(glyph)!;

                    if (rect.Width == 0 && rect.Height == 0)
                        continue;

                    if (rect.Left < minX) minX = rect.Left;
                    if (rect.Right > maxX) maxX = rect.Right;
                }

                if (maxX <= minX)
                    return;

                for (int i = 0; i < characters.Count; i++)
                {
                    object? glyph = characters[i];
                    var rect = (RectangleF)rectProperty.GetValue(glyph)!;

                    if (rect.Width == 0 && rect.Height == 0)
                        continue;

                    parts.Add(new GlyphPart
                    {
                        DrawQuad = GradientSource.ToScreenSpace(rect.Inflate(inflationAmount)),
                        InflationPercentage = new Vector2(
                            rect.Size.X == 0 ? 0 : inflationAmount.X / rect.Size.X,
                            rect.Size.Y == 0 ? 0 : inflationAmount.Y / rect.Size.Y),
                        Texture = (Texture)textureProperty.GetValue(glyph)!,
                        Progress = (rect.Centre.X - minX) / (maxX - minX),
                    });
                }
            }

            private void buildHideQuad()
            {
                if (parts.Count == 0)
                {
                    hideQuad = default;
                    return;
                }

                var min = parts[0].DrawQuad.TopLeft;
                var max = parts[0].DrawQuad.TopLeft;

                foreach (var part in parts)
                {
                    min = Vector2.ComponentMin(min, part.DrawQuad.TopLeft);
                    min = Vector2.ComponentMin(min, part.DrawQuad.TopRight);
                    min = Vector2.ComponentMin(min, part.DrawQuad.BottomLeft);
                    min = Vector2.ComponentMin(min, part.DrawQuad.BottomRight);
                    max = Vector2.ComponentMax(max, part.DrawQuad.TopLeft);
                    max = Vector2.ComponentMax(max, part.DrawQuad.TopRight);
                    max = Vector2.ComponentMax(max, part.DrawQuad.BottomLeft);
                    max = Vector2.ComponentMax(max, part.DrawQuad.BottomRight);
                }

                hideQuad = new Quad(min, new Vector2(max.X, min.Y), new Vector2(min.X, max.Y), max);
            }

            private struct GlyphPart
            {
                public Quad DrawQuad;
                public Vector2 InflationPercentage;
                public Texture Texture;
                public float Progress;
            }
        }
    }
}
