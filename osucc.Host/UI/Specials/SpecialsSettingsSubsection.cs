using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osucc.Client;
using osucc.Localisation;
using osucc.UI.Overlays;
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
            addCheckbox(ClientConfig.CelebrateNewRecord, SpecialsSettingsStrings.PersonalBestCaption, SpecialsSettingsStrings.PersonalBestHint);
            addCheckbox(ClientConfig.DisableSoloScoreSubmission, SpecialsSettingsStrings.DisableSoloScoreSubmissionCaption, SpecialsSettingsStrings.DisableSoloScoreSubmissionHint);
            addCheckbox(ClientConfig.ShowRandomModsButton, SpecialsSettingsStrings.RandomModsButtonCaption, SpecialsSettingsStrings.RandomModsButtonHint);
            addCheckbox(ClientConfig.SentryErrorReporting, SpecialsSettingsStrings.SentryErrorReportingCaption, SpecialsSettingsStrings.SentryErrorReportingHint);
            addCheckbox(ClientConfig.FavouriteMapHighlight, SpecialsSettingsStrings.FavouriteMapHighlightCaption, SpecialsSettingsStrings.FavouriteMapHighlightHint);
            addCheckbox(ClientConfig.ProfileFavouriteDownloadButton, SpecialsSettingsStrings.ProfileFavouriteDownloadButtonCaption, SpecialsSettingsStrings.ProfileFavouriteDownloadButtonHint);

            addThemeButton();

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

        /// <summary>
        /// Cosmetic-chrome theme picker. A button opens the live <see cref="ThemePreviewOverlay"/>
        /// (instead of the in-settings dropdown, which went out of sync with the preview's own
        /// selector); the theme is only persisted when the preview's apply button is pressed, which
        /// then confirms a restart.
        /// </summary>
        private void addThemeButton()
        {
            var btn = new SettingsButtonV2
            {
                Text = SpecialsSettingsStrings.ThemeCaption,
                TooltipText = SpecialsSettingsStrings.ThemeHint,
                Action = () => openPreview(),
            };
            
            osucc.Core.OsuCcThemeManager.IsActiveThemeDirty.BindValueChanged(change =>
            {
                btn.Text = change.NewValue 
                    ? new LocalisableString(SpecialsSettingsStrings.ThemeCaption.ToString() + " [DIRTY]") 
                    : SpecialsSettingsStrings.ThemeCaption;
            }, true);

            Add(btn);
        }

        /// <summary>
        /// Opens the theme preview overlay, first hiding the settings so the preview renders on top
        /// (settings lives in the leftFloating layer, above the preview overlay's overlayContent layer).
        /// </summary>
        private void openPreview()
        {
            if (ThemePreviewComponent.Instance == null)
            {
                ClientNotifications.Error(ThemePreviewStrings.ApplyFailed);
                return;
            }

            for (Drawable? current = this; current != null; current = current.Parent)
            {
                if (current is SettingsOverlay settingsOverlay)
                {
                    settingsOverlay.Hide();
                    break;
                }
            }

            ThemePreviewComponent.Instance.Show();
        }
    }
}
