using osu.Framework.Localisation;
using osu.Game;
using osuTK.Graphics;
using System;
using System.Collections.Generic;

namespace osucc.Core
{
    /// <summary>
    /// Manages the active theme and provides colour mapping functions for the runtime.
    /// </summary>
    public static class OsuCcThemeManager
    {
        private static readonly object lockObject = new();
        private static OsuCcThemeDefinition active;
        private static ThemePalette? activePalette; // null while the vanilla theme is active.

        public static readonly osu.Framework.Bindables.BindableBool IsActiveThemeDirty = new();

        static OsuCcThemeManager()
        {
            active = OsuCcThemeRegistry.Get(OsuCcThemeRegistry.DefaultId);
            activePalette = null; // vanilla themes bypass the chrome mapper entirely.
        }

        /// <summary>The currently active theme definition. Never null; vanilla by default.</summary>
        public static OsuCcThemeDefinition Active
        {
            get
            {
                lock (lockObject)
                    return active;
            }
        }

        /// <summary>The id of <see cref="Active"/>, as persisted in the client config.</summary>
        public static string ActiveId
        {
            get
            {
                lock (lockObject)
                    return active.Id;
            }
        }

        /// <summary>Whether the vanilla theme is active (no chrome conversion, accent transform is identity).</summary>
        public static bool IsVanillaActive
        {
            get
            {
                lock (lockObject)
                    return active.IsVanilla;
            }
        }

        /// <summary>All registered themes in registration order.</summary>
        public static IReadOnlyList<OsuCcThemeDefinition> RegisteredThemes => OsuCcThemeRegistry.RegisteredThemes;

        /// <summary>Switches the active theme by id. Throws for an unknown id.</summary>
        public static void SetActive(string id) => SetActive(OsuCcThemeRegistry.Get(id));

        /// <summary>Switches the active theme definition.</summary>
        public static void SetActive(OsuCcThemeDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            lock (lockObject)
            {
                active = definition;
                activePalette = definition.IsVanilla ? null : new ThemePalette(definition);
            }
        }

        /// <summary>
        /// Maps colour inputs through the active theme. Throws for the vanilla theme.
        /// </summary>
        public static Color4 MapChrome(float saturation, float lightness)
        {
            lock (lockObject)
            {
                if (activePalette == null)
                    throw new InvalidOperationException($"MapChrome cannot be used while the vanilla theme ({active.Id}) is active.");

                return activePalette.MapChrome(saturation, lightness);
            }
        }

        /// <summary>Transforms an arbitrary accent colour (game <c>OsuColour</c>, <see cref="OsuCcColours"/>) through <see cref="Active"/>.</summary>
        public static Color4 Transform(Color4 source)
        {
            lock (lockObject)
            {
                if (activePalette == null)
                    return source; // vanilla identity.

                return activePalette.Transform(source);
            }
        }

        /// <summary>
        /// Applies a theme to a running game. Repaints vanilla colours in place.
        /// </summary>
        public static void ApplyToGame(OsuGameBase? game, OsuCcThemeDefinition? theme = null)
        {
            lock (lockObject)
            {
                var definition = theme ?? active;
                var palette = definition.IsVanilla ? null : new ThemePalette(definition);

                Patches.OsuColourThemePatch.ApplyToGame(game, definition);
                OsuCcColours.ApplyTheme(definition);

                // Keep active in sync when a definition was passed explicitly (preview flow).
                if (theme != null)
                {
                    active = definition;
                    activePalette = palette;
                }

                IsActiveThemeDirty.Value = false;
            }
        }
    }
}