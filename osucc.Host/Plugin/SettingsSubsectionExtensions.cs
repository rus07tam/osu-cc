using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using System.Linq;

namespace osucc.Plugin
{
    /// <summary>
    /// Convenience controls for plugin settings subsections: the common V2 checkbox and a colour
    /// palette persisted as a comma-separated hex string via <see cref="PluginSettings"/>.
    /// </summary>
    public static class SettingsSubsectionExtensions
    {
        /// <summary>Adds a V2 checkbox bound to the plugin setting and returns it for further wiring.</summary>
        public static FormCheckBox AddCheckbox(this SettingsSubsection subsection, PluginSettings settings, string key, bool defaultValue, LocalisableString caption, LocalisableString hint)
        {
            var checkbox = new FormCheckBox
            {
                Caption = caption,
                HintText = hint,
                Current = settings.Bind(key, defaultValue),
            };

            subsection.Add(new SettingsItemV2(checkbox));
            return checkbox;
        }

        /// <summary>
        /// Adds a colour palette persisted as a comma-separated hex string, kept in sync both ways
        /// (any palette mutation saves; external value changes re-apply, guarding against the echo
        /// of our own writes).
        /// </summary>
        public static OsuCcColourPalette AddColourPalette(this SettingsSubsection subsection, PluginSettings settings, string key, LocalisableString caption, LocalisableString hint)
        {
            var persisted = settings.Bind(key, string.Empty);

            var palette = new OsuCcColourPalette
            {
                Caption = caption,
                HintText = hint,
                RelativeSizeAxes = Axes.X,
            };

            // OsuCcColourPalette is not an IFormControl, so it is added directly (it renders its
            // own caption/hint) instead of being wrapped in a SettingsItemV2 like the checkbox.
            // CompositeDrawable.Padding is protected, so the canonical inset lives on the wrapper.
            subsection.Add(new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = SettingsPanel.CONTENT_PADDING,
                Child = palette,
            });

            foreach (var colour in ParsePalette(persisted.Value))
                palette.Colours.Add(colour);

            // Persist any palette mutation back to the string store.
            palette.Colours.CollectionChanged += (_, _) =>
                persisted.Value = string.Join(",", palette.Colours.Select(c => c.ToHex()));

            // Re-apply the persisted string when it changes externally, guarding against the
            // echo back from our own writes (which round-trip to the same string).
            persisted.ValueChanged += e =>
            {
                if (string.Join(",", palette.Colours.Select(c => c.ToHex())) == e.NewValue)
                    return;

                palette.Colours.Clear();

                foreach (var colour in ParsePalette(e.NewValue))
                    palette.Colours.Add(colour);
            };

            return palette;
        }

        /// <summary>Parses a comma-separated hex colour string into a palette; unparseable entries are skipped.</summary>
        public static Colour4[] ParsePalette(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<Colour4>();

            var colours = new List<Colour4>();

            foreach (string part in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    colours.Add(Color4Extensions.FromHex(part));
                }
                catch
                {
                    // skip unparseable entries
                }
            }

            return colours.ToArray();
        }
    }
}
