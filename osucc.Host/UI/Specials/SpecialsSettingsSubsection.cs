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
            addCheckbox(ClientConfig.Branding, SpecialsSettingsStrings.BrandingCaption, default);
            addCheckbox(ClientConfig.AllowIncompatibleMods, SpecialsSettingsStrings.AllowIncompatibleModsCaption, SpecialsSettingsStrings.AllowIncompatibleModsHint);
            addCheckbox(ClientConfig.ShowSystemMods, SpecialsSettingsStrings.ShowSystemModsCaption, SpecialsSettingsStrings.ShowSystemModsHint);
            addCheckbox(ClientConfig.FirstRunSetupComplete, SpecialsSettingsStrings.FirstRunSetupCompleteCaption, SpecialsSettingsStrings.FirstRunSetupCompleteHint);
            addCheckbox(ClientConfig.CelebrateNewRecord, SpecialsSettingsStrings.CelebrateNewRecordCaption, SpecialsSettingsStrings.CelebrateNewRecordHint);
            addCheckbox(ClientConfig.DisableSoloScoreSubmission, SpecialsSettingsStrings.DisableSoloScoreSubmissionCaption, SpecialsSettingsStrings.DisableSoloScoreSubmissionHint);
            addCheckbox(ClientConfig.ShowRandomModsButton, SpecialsSettingsStrings.RandomModsButtonCaption, SpecialsSettingsStrings.RandomModsButtonHint);
            addCheckbox(ClientConfig.SentryErrorReporting, SpecialsSettingsStrings.SentryErrorReportingCaption, SpecialsSettingsStrings.SentryErrorReportingHint);
            addCheckbox(ClientConfig.FavouriteMapHighlight, SpecialsSettingsStrings.FavouriteMapHighlightCaption, SpecialsSettingsStrings.FavouriteMapHighlightHint);
            addCheckbox(ClientConfig.ProfileFavouriteDownloadButton, SpecialsSettingsStrings.ProfileFavouriteDownloadButtonCaption, SpecialsSettingsStrings.ProfileFavouriteDownloadButtonHint);

            var supporterEnabled = addCheckbox(ClientConfig.FakeSupporterEnabled, SpecialsSettingsStrings.FakeSupporterEnabledCaption, SpecialsSettingsStrings.FakeSupporterEnabledHint);

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
                    Value = ClientConfig.FakeSupporterLevel.Value,
                },
            };
            supporterLevel.Current.BindValueChanged(e => ClientConfig.FakeSupporterLevel.Value = e.NewValue, true);
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
