using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;
using osucc.Plugin;

namespace ExamplePlugin
{
    /// <summary>
    /// Settings subsection injected into the "Specials" section. Every control binds to the
    /// plugin's own <see cref="PluginSettings"/>, persisted under the game storage. Uses the
    /// V2 <see cref="SettingsItemV2"/> control to match the modern settings look.
    /// </summary>
    public partial class ExampleSettingsSubsection : SettingsSubsection
    {
        protected override LocalisableString Header => ExamplePluginStrings.Name;

        public ExampleSettingsSubsection(PluginSettings settings)
        {
            this.AddCheckbox(settings, "celebrate", true, ExamplePluginStrings.CelebrateCaption, ExamplePluginStrings.CelebrateHint);
        }
    }
}
