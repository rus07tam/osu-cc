using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;
using osucc.Client;
using osucc.Localisation;

namespace osucc.UI.Specials
{
    /// <summary>
    /// Subsection in the Specials section for experimental, unstable settings that take effect only after restarting the game.
    /// </summary>
    public partial class ExperimentsSettingsSubsection : SettingsSubsection
    {
        protected override LocalisableString Header => ExperimentsSettingsStrings.SubsectionHeader;

        public ExperimentsSettingsSubsection()
        {
            addCheckbox(ClientConfig.LivePluginReloading, ExperimentsSettingsStrings.LivePluginReloadingCaption, ExperimentsSettingsStrings.LivePluginReloadingHint);
            addCheckbox(ClientConfig.BypassHostDependencyCheck, ExperimentsSettingsStrings.BypassHostDependencyCheckCaption, ExperimentsSettingsStrings.BypassHostDependencyCheckHint);
            addCheckbox(ClientConfig.BypassPluginDependencyCheck, ExperimentsSettingsStrings.BypassPluginDependencyCheckCaption, ExperimentsSettingsStrings.BypassPluginDependencyCheckHint);
        }

        private FormCheckBox addCheckbox(Bindable<bool> current, LocalisableString caption, LocalisableString hint)
        {
            var checkbox = new FormCheckBox
            {
                Caption = caption,
                HintText = hint,
                Current = current,
            };

            Add(new SettingsItemV2(checkbox));
            return checkbox;
        }
    }
}
