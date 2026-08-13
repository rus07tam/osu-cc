using osu.Framework.Graphics;
using osucc.Client;
using osucc.Core;
using osuTK.Graphics;
using System;
using System.Linq;
using System.Reflection;

namespace osucc.Patches
{
    public static class OsuColourThemePatch
    {
        // Snapshots of the resolved OsuColour instance's vanilla field values. Re-painting mutates
        // the fields in place, so to re-theme repeatedly (theme preview) we must always start from
        // the original values rather than the already-transformed ones, or the transform compounds.
        private static object? cachedInstance;
        private static readonly List<(FieldInfo Field, Color4 Base)> cachedFields = new();

        /// <summary>
        /// Captures the resolved <c>OsuColour</c> instance and its vanilla field values once, so
        /// subsequent <see cref="ApplyToGame"/> calls can re-theme from a stable baseline. Call this
        /// before any theming is applied.
        /// </summary>
        public static void CaptureBase(osu.Game.OsuGameBase game)
        {
            if (cachedInstance != null)
                return;

            if (game?.Dependencies == null)
                return;

            var type = Reflection.GetGameType("osu.Game.Graphics.OsuColour");
            if (type == null)
                return;

            object instance;
            try
            {
                instance = game.Dependencies.Get(type);
            }
            catch
            {
                return;
            }

            if (instance == null)
                return;

            cachedInstance = instance;

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                             .Where(f => f.FieldType == typeof(Color4));

            foreach (var field in fields)
            {
                try
                {
                    object? raw = field.IsStatic ? field.GetValue(null) : field.GetValue(instance);
                    if (raw is Color4 colour)
                        cachedFields.Add((field, colour));
                }
                catch
                {
                    // skip fields we cannot read
                }
            }

            TimingLog.Info($"OsuColourThemePatch: captured {cachedFields.Count} vanilla colour field(s)");
        }

        /// <summary>
        /// Re-paints the <c>OsuColour</c> colour fields through the given theme's transform, restoring
        /// them to their vanilla baseline first. Uses the theme managed by
        /// <see cref="OsuCcThemeManager"/> when <paramref name="theme"/> is <c>null</c>.
        /// </summary>
        public static void ApplyToGame(osu.Game.OsuGameBase? game, osucc.Core.OsuCcThemeDefinition? theme = null)
        {
            osucc.Core.OsuCcThemeDefinition target = theme ?? osucc.Core.OsuCcThemeManager.Active;

            if (target.IsVanilla)
            {
                // Revert to vanilla when switching back to the default theme.
                if (cachedInstance != null)
                    repaintFromBase(cachedInstance, target);
                return;
            }

            if (cachedInstance == null && game != null)
                CaptureBase(game);

            if (cachedInstance != null)
                repaintFromBase(cachedInstance, target);
        }

        private static void repaintFromBase(object instance, osucc.Core.OsuCcThemeDefinition theme)
        {
            var palette = theme.IsVanilla ? null : new osucc.Core.ThemePalette(theme);
            int painted = 0;

            foreach (var (field, baseColour) in cachedFields)
            {
                try
                {
                    var transformed = palette == null ? baseColour : palette.Transform(baseColour);

                    if (field.IsStatic)
                        field.SetValue(null, transformed);
                    else
                        field.SetValue(instance, transformed);

                    painted++;
                }
                catch (Exception)
                {
                    // skip fields that cannot be re-painted
                }
            }

            TimingLog.Info($"OsuColourThemePatch: repainted {painted} colour field(s) for {theme.Id}");
        }
    }
}
