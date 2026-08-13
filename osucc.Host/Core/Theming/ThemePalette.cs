using osuTK.Graphics;
using osucc.Core;
using System;

namespace osucc.Core
{
    /// <summary>
    /// Stateless HSL/RGB colour math shared by the theming engine. The original osu. framework
    /// <c>OsuColour</c>-style conversions are reimplemented here so the chrome mappings can build
    /// hues in degrees, mirroring the logic previously living in the old <c>OsuCcThemePalette</c>.
    /// </summary>
    public static class ThemeColourMath
    {
        /// <summary>Converts an sRGB <see cref="Color4"/> to HSL (h/s/l in 0..1 ranges).</summary>
        public static (float H, float S, float L) RgbToHsl(Color4 c)
        {
            float r = c.R, g = c.G, b = c.B;
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            float h, s;
            float l = (max + min) / 2f;

            if (Math.Abs(delta) < 1e-6f)
            {
                h = 0f;
                s = 0f;
            }
            else
            {
                s = l > 0.5f ? delta / (2f - max - min) : delta / (max + min);

                float dr = (((max - r) / 6f) + (delta / 2f)) / delta;
                float dg = (((max - g) / 6f) + (delta / 2f)) / delta;
                float db = (((max - b) / 6f) + (delta / 2f)) / delta;

                if (r == max) h = db - dg;
                else if (g == max) h = (1f / 3f) + dr - db;
                else h = (2f / 3f) + dg - dr;

                if (h < 0f) h += 1f;
                if (h > 1f) h -= 1f;
            }

            return (h, s, l);
        }

        /// <summary>Converts HSL (h/s/l in 0..1 ranges, alpha 0..1) to an sRGB <see cref="Color4"/>.</summary>
        public static Color4 HslToRgb(float h, float s, float l, float alpha = 1f)
        {
            s = Math.Clamp(s, 0f, 1f);
            l = Math.Clamp(l, 0f, 1f);

            if (s < 1e-6f)
                return new Color4(l, l, l, alpha);

            h = ((h % 1f) + 1f) % 1f;

            float q = l < 0.5f ? l * (1f + s) : l + s - (l * s);
            float p = 2f * l - q;

            return new Color4(
                hueToRgb(p, q, h + (1f / 3f)),
                hueToRgb(p, q, h),
                hueToRgb(p, q, h - (1f / 3f)),
                alpha);
        }

        private static float hueToRgb(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
            if (t < 1f / 6f) return p + ((q - p) * 6f * t);
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + ((q - p) * (2f / 3f - t) * 6f);
            return p;
        }

        /// <summary>Luminance-preserving grayscale (ITU-R BT.601 luma coefficients).</summary>
        public static Color4 Desaturate(Color4 source)
        {
            float luma = 0.299f * source.R + 0.587f * source.G + 0.114f * source.B;
            return new Color4(luma, luma, luma, source.A);
        }

        /// <summary>Converts a hue in degrees to the 0..1 hue used by the HSL conversions.</summary>
        public static float HueDegreesToUnit(float degrees) => (degrees % 360f) / 360f;
    }

    /// <summary>
    /// Stateless engine that produces themed chrome colours from an <see cref="OsuCcThemeDefinition"/>.
    /// One instance per definition (registry-built themes); thread-safe by construction, so the
    /// runtime <c>OverlayColourProvider</c> patch can build a palette against
    /// <see cref="OsuCcThemeManager.Active"/> on every call.
    /// </summary>
    public sealed class ThemePalette
    {
        public OsuCcThemeDefinition Definition { get; }

        public ThemePalette(OsuCcThemeDefinition definition)
        {
            Definition = definition;
        }

        /// <summary>
        /// Maps the <c>OverlayColourProvider.getColour(saturation, lightness)</c> inputs to the themed
        /// colour, using the same (text / accent / surface) banding the vanilla call sites produce.
        /// </summary>
        public Color4 MapChrome(float saturation, float lightness)
        {
            var chrome = Definition.Chrome;

            if (lightness >= chrome.TextLightnessThreshold)
                return mapText(chrome);

            if (saturation >= chrome.AccentSaturationThreshold)
                return mapAccent(chrome.Accent, saturation, lightness);

            return mapSurface(chrome.Surface, saturation, lightness);
        }

        /// <summary>Applies the theme's accent transform to an arbitrary source colour.</summary>
        public Color4 Transform(Color4 source)
        {
            switch (Definition.AccentTransform.Kind)
            {
                case AccentTransformKind.Desaturate:
                    return ThemeColourMath.Desaturate(source);

                case AccentTransformKind.HueShift:
                {
                    var (_, sat, light) = ThemeColourMath.RgbToHsl(source);
                    return ThemeColourMath.HslToRgb(
                        ThemeColourMath.HueDegreesToUnit(Definition.AccentTransform.HueDegrees ?? 0f),
                        sat,
                        light,
                        source.A);
                }

                default:
                    return source;
            }
        }

        private static Color4 mapText(ChromeRampDefinition chrome)
        {
            var text = chrome.Text;
            return ThemeColourMath.HslToRgb(ThemeColourMath.HueDegreesToUnit(text.HueDegrees), text.Saturation, text.Lightness);
        }

        private static Color4 mapAccent(AccentBandDefinition accent, float saturation, float lightness)
        {
            float sat = accent.Saturation == SaturationMode.Keep ? saturation : accent.FixedSaturation;
            float light = Math.Clamp(lightness, accent.LightnessMin, accent.LightnessMax);

            return ThemeColourMath.HslToRgb(ThemeColourMath.HueDegreesToUnit(accent.HueDegrees), sat, light);
        }

        private static Color4 mapSurface(SurfaceBandDefinition surface, float saturation, float lightness)
        {
            float sat = surface.Saturation == SaturationMode.Keep ? saturation : surface.FixedSaturation;

            float outLightness = surface.KeepSourceAboveSaturation.HasValue && saturation > surface.KeepSourceAboveSaturation.Value
                ? lightness
                : surface.Lightness.Map(lightness);

            return ThemeColourMath.HslToRgb(ThemeColourMath.HueDegreesToUnit(surface.HueDegrees), sat, outLightness);
        }
    }
}