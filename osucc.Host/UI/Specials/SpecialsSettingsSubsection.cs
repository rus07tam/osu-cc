using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osucc.Client;
using osucc.Localisation;
using osucc.UI.Plugins;

namespace osucc.UI.Specials
{
    /// <summary>
    /// Settings items for the osucc client, all bound through <see cref="ClientApi"/>. Uses the V2
    /// controls so the section matches the modern settings look.
    /// </summary>
    public partial class SpecialsSettingsSubsection : SettingsSubsection
    {
        protected override LocalisableString Header => SpecialsSettingsStrings.SubsectionHeader;

        public SpecialsSettingsSubsection()
        {
            var config = ClientApi.Config;

            if (config == null)
                return;

            addCheckbox(config, SpecialsSettingsStrings.BrandingCaption, default, SpecialsSetting.Branding);
            addCheckbox(config, SpecialsSettingsStrings.AllowIncompatibleModsCaption, SpecialsSettingsStrings.AllowIncompatibleModsHint, SpecialsSetting.AllowIncompatibleMods);
            addCheckbox(config, SpecialsSettingsStrings.ShowSystemModsCaption, SpecialsSettingsStrings.ShowSystemModsHint, SpecialsSetting.ShowSystemMods);
            addCheckbox(config, SpecialsSettingsStrings.FirstRunSetupCompleteCaption, SpecialsSettingsStrings.FirstRunSetupCompleteHint, SpecialsSetting.FirstRunSetupComplete);
            addCheckbox(config, SpecialsSettingsStrings.CelebrateNewRecordCaption, SpecialsSettingsStrings.CelebrateNewRecordHint, SpecialsSetting.CelebrateNewRecord);
            addCheckbox(config, SpecialsSettingsStrings.DisableSoloScoreSubmissionCaption, SpecialsSettingsStrings.DisableSoloScoreSubmissionHint, SpecialsSetting.DisableSoloScoreSubmission);
            addCheckbox(config, SpecialsSettingsStrings.RandomModsButtonCaption, SpecialsSettingsStrings.RandomModsButtonHint, SpecialsSetting.ShowRandomModsButton);
            addCheckbox(config, SpecialsSettingsStrings.SentryErrorReportingCaption, SpecialsSettingsStrings.SentryErrorReportingHint, SpecialsSetting.SentryErrorReporting);
            addCheckbox(config, SpecialsSettingsStrings.FavouriteMapHighlightCaption, SpecialsSettingsStrings.FavouriteMapHighlightHint, SpecialsSetting.FavouriteMapHighlight);
            addCheckbox(config, SpecialsSettingsStrings.ProfileFavouriteDownloadButtonCaption, SpecialsSettingsStrings.ProfileFavouriteDownloadButtonHint, SpecialsSetting.ProfileFavouriteDownloadButton);

            var supporterEnabled = addCheckbox(config, SpecialsSettingsStrings.FakeSupporterEnabledCaption, SpecialsSettingsStrings.FakeSupporterEnabledHint, SpecialsSetting.FakeSupporterEnabled);

            // The slider needs a BindableNumber range (1–10), while the config exposes a plain
            // Bindable<int>; mirror its value both ways. TransferValueOnCommit (as osu's own
            // settings sliders use) keeps LoadComplete from writing the instantaneous value into
            // the Disabled bindable when the fake supporter is switched off — that write would
            // otherwise throw.
            var supporterLevel = new FormSliderBar<int>
            {
                Caption = SpecialsSettingsStrings.FakeSupporterLevelCaption,
                HintText = SpecialsSettingsStrings.FakeSupporterLevelHint,
                TransferValueOnCommit = true,
                Current = new BindableNumber<int>
                {
                    MinValue = 1,
                    MaxValue = 10,
                    Value = config.GetBindable<int>(SpecialsSetting.FakeSupporterLevel).Value,
                },
            };
            supporterLevel.Current.BindValueChanged(e => config.GetBindable<int>(SpecialsSetting.FakeSupporterLevel).Value = e.NewValue, true);
            Add(new SettingsItemV2(supporterLevel));

            supporterEnabled.Current.BindValueChanged(e => supporterLevel.Current.Disabled = !e.NewValue, true);

            Add(new SettingsButtonV2
            {
                Text = SpecialsSettingsStrings.ManagePluginsCaption,
                TooltipText = SpecialsSettingsStrings.ManagePluginsTooltip,
                Action = () =>
                {
                    // Settings lives in the leftFloating layer, above the plugins overlay's
                    // overlayContent layer; close it so the manager opens on top.
                    for (Drawable? current = this; current != null; current = current.Parent)
                    {
                        if (current is SettingsOverlay settingsOverlay)
                        {
                            settingsOverlay.Hide();
                            break;
                        }
                    }

                    PluginsOverlayComponent.Instance?.Toggle();
                },
            });
        }

        private FormCheckBox addCheckbox(SpecialsConfigManager config, LocalisableString caption, LocalisableString hint, SpecialsSetting setting)
        {
            var checkbox = new FormCheckBox
            {
                Caption = caption,
                HintText = hint,
                Current = config.GetBindable<bool>(setting),
            };

            Add(new SettingsItemV2(checkbox));
            return checkbox;
        }
    }
}
