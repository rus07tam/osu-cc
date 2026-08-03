using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;
using osucc.Plugin;

namespace UsernameVisuals
{
    /// <summary>
    /// Settings subsection injected into the "Specials" section: a master toggle plus two colour
    /// palettes (own username / everyone else), persisted as comma-separated hex strings via
    /// <see cref="PluginSettings"/>, an own-username display override (custom text / hide), and
    /// per-user colour / display overrides for specific users.
    /// </summary>
    public partial class UsernameVisualsSettingsSubsection : SettingsSubsection
    {
        protected override LocalisableString Header => UsernameVisualsStrings.Name;

        public UsernameVisualsSettingsSubsection(PluginSettings settings, UsernameVisualsApi api)
        {
            this.AddCheckbox(settings, "gradient_enabled", false, UsernameVisualsStrings.GradientEnabledCaption, UsernameVisualsStrings.GradientEnabledHint);
            this.AddColourPalette(settings, "self_palette", UsernameVisualsStrings.SelfPaletteCaption, UsernameVisualsStrings.SelfPaletteHint);
            this.AddColourPalette(settings, "others_palette", UsernameVisualsStrings.OthersPaletteCaption, UsernameVisualsStrings.OthersPaletteHint);

            this.AddCheckbox(settings, "own_replace_enabled", false, UsernameVisualsStrings.ReplaceEnabledCaption, UsernameVisualsStrings.ReplaceEnabledHint);
            Add(new SettingsItemV2(new FormTextBox
            {
                Caption = UsernameVisualsStrings.DisplayNameCaption,
                PlaceholderText = UsernameVisualsStrings.DisplayNamePlaceholder,
                Current = settings.Bind("own_replace_name", string.Empty),
            }));

            this.AddCheckbox(settings, "own_hide_enabled", false, UsernameVisualsStrings.HideEnabledCaption, UsernameVisualsStrings.HideEnabledHint);

            Add(new UsernameVisualsUserOverridesSection(api));
        }
    }
}
