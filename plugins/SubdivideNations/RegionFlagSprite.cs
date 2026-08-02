using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osuTK;

namespace SubdivideNations
{
    /// <summary>
    /// A region flag drawn with the same frame and proportions as osu's country flag: masked rounded
    /// corners (the country flag texture bakes a ~10%-of-width corner radius) and a 150:108 box.
    /// The flag fits the frame whole with its real aspect preserved (letterboxed when the aspect
    /// differs), so arbitrary region flag images never get distorted or over-cropped.
    /// Optional region-name tooltip.
    /// </summary>
    public partial class RegionFlagSprite : Container, IHasTooltip
    {
        /// <summary>Country flag corner radius relative to its texture width (15px of 150px).</summary>
        private const float cornerRadiusScale = 15f / 150f;

        /// <summary>Country flag proportions (<c>UpdateableFlag</c> panels use a 36x26 box).</summary>
        public const float AspectRatio = 150f / 108f;

        public LocalisableString TooltipText { get; set; }

        public RegionFlagSprite(Texture texture, string? regionName = null)
        {
            if (regionName != null)
                TooltipText = regionName;

            Masking = true;
            Child = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(1),
                Texture = texture,
                FillMode = FillMode.Fit,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            };
        }

        protected override void Update()
        {
            base.Update();

            float target = Width * cornerRadiusScale;
            if (CornerRadius != target)
                CornerRadius = target;
        }
    }
}
