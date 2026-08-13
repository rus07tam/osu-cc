using osu.Framework.Extensions.Color4Extensions;
using osucc.Core;
using osuTK.Graphics;
using System;
using System.Linq;
using System.Reflection;

namespace osucc.Core
{
    /// <summary>
    /// Shared colour palette for osu!cc surfaces, mirroring the game's
    /// <see cref="osu.Game.Graphics.OsuColour"/> values so toasts and status text match the stock styling.
    ///
    /// The fields are kept as plain <see cref="Color4"/> statics for binary compatibility with
    /// already-built plugins (they read these as <c>ldsfld</c>). To apply the theme
    /// <see cref="OsuCcThemeManager.Active"/>, <see cref="ApplyTheme"/> re-paints every field in
    /// place through <see cref="ThemePalette.Transform"/> at startup — the same technique the
    /// client uses for the game's own <c>OsuColour</c>.
    /// </summary>
    public static class OsuCcColours
    {
#pragma warning disable CA2211 // Non-constant fields are deliberate: ApplyTheme re-paints them for the active UI theme.
        /// <summary>OsuColour.Green</summary>
        public static Color4 Success = Color4Extensions.FromHex("88b300");

        /// <summary>OsuColour.Red</summary>
        public static Color4 Error = Color4Extensions.FromHex("ed1121");

        /// <summary>OsuColour.Cyan</summary>
        public static Color4 Info = Color4Extensions.FromHex("05f4fd");

        /// <summary>OsuColour.Yellow</summary>
        public static Color4 Warning = Color4Extensions.FromHex("ffcc22");

        /// <summary>neutral grey</summary>
        public static Color4 Disabled = Color4Extensions.FromHex("d3d3d3");

        /// <summary>OsuColour.Pink</summary>
        public static Color4 Pink = Color4Extensions.FromHex("ff66aa");
#pragma warning restore CA2211

        // Vanilla field snapshot so repeated ApplyTheme calls (theme preview) start from a stable
        // baseline instead of compounding the transform on already-transformed values.
        private static Color4[]? baseValues;

        /// <summary>Ensures the vanilla baseline is captured; the returned array is the stable snapshot.</summary>
        public static Color4[] BaseValues
        {
            get
            {
                if (baseValues == null)
                    baseValues = captureBase();
                return baseValues;
            }
        }

        /// <summary>
        /// Re-paints every <see cref="Color4"/> field on this type through the theme registered as
        /// <see cref="OsuCcThemeManager.Active"/>. Called once at startup after the active theme has
        /// been pinned; a no-op for the vanilla theme.
        /// </summary>
        public static void ApplyTheme() => ApplyTheme(OsuCcThemeManager.Active);

        /// <summary>
        /// Re-paints every <see cref="Color4"/> field on this type through the given theme's
        /// transform, always starting from the vanilla baseline so repeated calls (theme preview)
        /// don't compound. For the vanilla theme the fields are restored as-is.
        /// </summary>
        public static void ApplyTheme(OsuCcThemeDefinition theme)
        {
            var baseValues = BaseValues;
            var palette = theme.IsVanilla ? null : new ThemePalette(theme);

            int painted = 0;
            int failed = 0;

            int index = 0;
            foreach (var field in typeof(OsuCcColours).GetFields(BindingFlags.Static | BindingFlags.Public)
                                                       .Where(f => f.FieldType == typeof(Color4)))
            {
                try
                {
                    var baseColour = baseValues.Length > index ? baseValues[index] : (Color4)field.GetValue(null)!;
                    var transformed = palette == null ? baseColour : palette.Transform(baseColour);

                    field.SetValue(null, transformed);
                    painted++;
                }
                catch (Exception ex)
                {
                    failed++;
                    TimingLog.Error($"OsuCcColours.ApplyTheme: failed on '{field.Name}': {ex.Message}");
                }

                index++;
            }

            TimingLog.Info($"OsuCcColours themed: {painted} colour(s) repainted, {failed} failed ({theme.Id})");
        }

        private static Color4[] captureBase()
            => typeof(OsuCcColours).GetFields(BindingFlags.Static | BindingFlags.Public)
                                   .Where(f => f.FieldType == typeof(Color4))
                                   .Select(f => (Color4)f.GetValue(null)!)
                                   .ToArray();
    }
}
