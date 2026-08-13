using osuTK;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace osucc.Core
{
    /// <summary>
    /// Defines a cosmetic UI theme, mapping the game's original colours to new ones.
    /// Used by ThemePalette to apply the theme at runtime.
    /// </summary>
    public sealed class OsuCcThemeDefinition
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }

        public bool IsVanilla { get; init; }

        public required AccentTransformDefinition AccentTransform { get; init; }
        public required ChromeRampDefinition Chrome { get; init; }

        public override string ToString() => Id;
    }

    public sealed class AccentTransformDefinition
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AccentTransformKind Kind { get; init; } = AccentTransformKind.Identity;
        public float? HueDegrees { get; init; }

        public static readonly AccentTransformDefinition Identity = new();
        public static AccentTransformDefinition HueShift(float degrees)
            => new AccentTransformDefinition { Kind = AccentTransformKind.HueShift, HueDegrees = degrees };
        public static readonly AccentTransformDefinition Desaturate = new() { Kind = AccentTransformKind.Desaturate };
    }

    public enum AccentTransformKind { Identity, Desaturate, HueShift }

    public sealed class ChromeRampDefinition
    {
        public float TextLightnessThreshold { get; init; } = 0.9f;
        public required HslSpec Text { get; init; }
        public float AccentSaturationThreshold { get; init; } = 0.4f;
        public required AccentBandDefinition Accent { get; init; }
        public required SurfaceBandDefinition Surface { get; init; }
    }

    public readonly record struct HslSpec(float HueDegrees, float Saturation, float Lightness);

    public enum SaturationMode { Keep, Fixed }

    public sealed class AccentBandDefinition
    {
        public required float HueDegrees { get; init; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SaturationMode Saturation { get; init; } = SaturationMode.Keep;
        public float FixedSaturation { get; init; }
        public float LightnessMin { get; init; } = 0.3f;
        public float LightnessMax { get; init; } = 0.85f;
    }

    public sealed class SurfaceBandDefinition
    {
        public required float HueDegrees { get; init; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SaturationMode Saturation { get; init; } = SaturationMode.Keep;
        public float FixedSaturation { get; init; }
        public required LightnessCurve Lightness { get; init; }
        public float? KeepSourceAboveSaturation { get; init; }
    }

    public sealed class LightnessCurve
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LightnessCurveInterpolation Interpolation { get; init; } = LightnessCurveInterpolation.Linear;
        public required IReadOnlyList<Vector2> ControlPoints { get; init; }
        public float? ClampMin { get; init; }
        public float? ClampMax { get; init; }

        public static readonly LightnessCurve Identity = new()
        {
            ControlPoints = new Vector2[] { new(0, 0), new(1, 1) },
        };

        public float Map(float source)
        {
            float result;
            switch (Interpolation)
            {
                case LightnessCurveInterpolation.Step:
                    result = ControlPoints[0].Y;
                    foreach (var point in ControlPoints)
                        if (source >= point.X) result = point.Y;
                    break;
                default:
                case LightnessCurveInterpolation.Linear:
                    if (source <= ControlPoints[0].X)
                    {
                        result = ControlPoints[0].Y;
                        break;
                    }
                    for (int i = 1; i < ControlPoints.Count; i++)
                    {
                        var next = ControlPoints[i];
                        if (source > next.X) continue;
                        var prev = ControlPoints[i - 1];
                        float t = (source - prev.X) / Math.Max(1e-6f, next.X - prev.X);
                        result = prev.Y + (next.Y - prev.Y) * t;
                        goto computed;
                    }
                    result = ControlPoints[^1].Y;
                    break;
                computed:
                    break;
            }

            if (ClampMin.HasValue) result = Math.Max(ClampMin.Value, result);
            if (ClampMax.HasValue) result = Math.Min(ClampMax.Value, result);
            return result;
        }
    }

    public enum LightnessCurveInterpolation { Linear, Step }
}
