using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;
using osucc.Client;
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

        public ExampleSettingsSubsection(PluginSettings settings, IOsuCcPluginHost host)
        {
            this.AddCheckbox(settings, "celebrate", true, ExamplePluginStrings.CelebrateCaption, ExamplePluginStrings.CelebrateHint);
            this.AddCheckbox(settings, "username_visuals_integration", false, ExamplePluginStrings.UsernameVisualsIntegrationCaption, ExamplePluginStrings.UsernameVisualsIntegrationHint);

            Add(new SettingsButtonV2
            {
                Text = ExamplePluginStrings.DialogButton,
                Action = () => host.Confirm(
                    ExamplePluginStrings.DialogTitle,
                    ExamplePluginStrings.DialogBody,
                    () => host.Notify(ExamplePluginStrings.DialogConfirmed, NotificationKind.Success)),
            });
        }
    }
}
