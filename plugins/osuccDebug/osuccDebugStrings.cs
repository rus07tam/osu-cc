using osu.Framework.Localisation;
using osucc.Localisation;

namespace osuccDebug
{
    public static class osuccDebugStrings
    {
        private const string prefix = "osucc-debug";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString Name => OsuCcLocalisation.Get($"{prefix}:name", "Debug Overlay");

        public static LocalisableString Description => OsuCcLocalisation.Get($"{prefix}:description", "The debug overlay: customised test overlays and notifications.");

        public static LocalisableString OverlayTitle => OsuCcLocalisation.Get(getKey(nameof(OverlayTitle)), "osu!cc debug");

        public static LocalisableString OverlayDescription => OsuCcLocalisation.Get(getKey(nameof(OverlayDescription)), "Customised test overlays and notifications");

        public static LocalisableString TooltipMain => OsuCcLocalisation.Get(getKey(nameof(TooltipMain)), "osu!cc debug");

        public static LocalisableString TooltipSub => OsuCcLocalisation.Get(getKey(nameof(TooltipSub)), "Customised test overlays and notifications");

        public static LocalisableString NotificationsPanelTitle => OsuCcLocalisation.Get(getKey(nameof(NotificationsPanelTitle)), "Notifications");

        public static LocalisableString DialogsPanelTitle => OsuCcLocalisation.Get(getKey(nameof(DialogsPanelTitle)), "Dialogs");

        public static LocalisableString PersonalBestPanelTitle => OsuCcLocalisation.Get(getKey(nameof(PersonalBestPanelTitle)), "Personal best");

        public static LocalisableString OverlaysPanelTitle => OsuCcLocalisation.Get(getKey(nameof(OverlaysPanelTitle)), "Overlays");

        public static LocalisableString CustomTitleLabel => OsuCcLocalisation.Get(getKey(nameof(CustomTitleLabel)), "Custom title");

        public static LocalisableString ShowWaveOverlayButton => OsuCcLocalisation.Get(getKey(nameof(ShowWaveOverlayButton)), "Show wave overlay");

        public static LocalisableString ShowShearedOverlayButton => OsuCcLocalisation.Get(getKey(nameof(ShowShearedOverlayButton)), "Show sheared overlay");

        public static LocalisableString WaveOverlayTitle => OsuCcLocalisation.Get(getKey(nameof(WaveOverlayTitle)), "Wave overlay");

        public static LocalisableString WaveOverlayColourSchemeLabel => OsuCcLocalisation.Get(getKey(nameof(WaveOverlayColourSchemeLabel)), "Colour scheme");

        public static LocalisableString WaveOverlayDescription => OsuCcLocalisation.Get(getKey(nameof(WaveOverlayDescription)), "Full-screen wave-style overlay test");

        public static LocalisableString WaveOverlaySectionTitle => OsuCcLocalisation.Get(getKey(nameof(WaveOverlaySectionTitle)), "Wave overlay");

        public static LocalisableString WaveOverlayBodyText => OsuCcLocalisation.Get(getKey(nameof(WaveOverlayBodyText)), "The coloured bands sweep over the dimmed background while the page (a stock-style header with icon, title, description and tabs, plus a scrollable main area) fades in on top. Close via the back key or clicking outside.");

        public static LocalisableString ShearedOverlayTitle => OsuCcLocalisation.Get(getKey(nameof(ShearedOverlayTitle)), "Sheared overlay");

        public static LocalisableString ShearedOverlayDescription => OsuCcLocalisation.Get(getKey(nameof(ShearedOverlayDescription)), "Full-screen sheared-style overlay test");

        public static LocalisableString ShearedOverlayBodyText => OsuCcLocalisation.Get(getKey(nameof(ShearedOverlayBodyText)), "The sheared-style overlay features an animated sheared header with close button, dimmed background, and a scrollable content area. Close via the header close button, back key or clicking outside.");

        public static LocalisableString ShearedOverlayNotifyButton => OsuCcLocalisation.Get(getKey(nameof(ShearedOverlayNotifyButton)), "Post notification from here");

        public static LocalisableString ShearedOverlayNotified => OsuCcLocalisation.Get(getKey(nameof(ShearedOverlayNotified)), "notification posted from the sheared overlay");

        public static LocalisableString WaveOverlayOverviewTab => OsuCcLocalisation.Get(getKey(nameof(WaveOverlayOverviewTab)), "Overview");

        public static LocalisableString WaveOverlayColoursTab => OsuCcLocalisation.Get(getKey(nameof(WaveOverlayColoursTab)), "Colours");

        public static LocalisableString WaveOverlayWaveBandsLabel => OsuCcLocalisation.Get(getKey(nameof(WaveOverlayWaveBandsLabel)), "Wave band colours");

        public static LocalisableString WaveOverlayNotifyButton => OsuCcLocalisation.Get(getKey(nameof(WaveOverlayNotifyButton)), "Post notification from here");

        public static LocalisableString WaveOverlayNotified => OsuCcLocalisation.Get(getKey(nameof(WaveOverlayNotified)), "notification posted from the wave overlay");

        public static LocalisableString WaveBandLight4 => OsuCcLocalisation.Get(getKey(nameof(WaveBandLight4)), "Light4");

        public static LocalisableString WaveBandLight3 => OsuCcLocalisation.Get(getKey(nameof(WaveBandLight3)), "Light3");

        public static LocalisableString WaveBandDark4 => OsuCcLocalisation.Get(getKey(nameof(WaveBandDark4)), "Dark4");

        public static LocalisableString WaveBandDark3 => OsuCcLocalisation.Get(getKey(nameof(WaveBandDark3)), "Dark3");

        public static LocalisableString NotificationMessageLabel => OsuCcLocalisation.Get(getKey(nameof(NotificationMessageLabel)), "Notification message");

        public static LocalisableString NotificationKindLabel => OsuCcLocalisation.Get(getKey(nameof(NotificationKindLabel)), "Notification kind");

        public static LocalisableString PostNotificationButton => OsuCcLocalisation.Get(getKey(nameof(PostNotificationButton)), "Post notification");

        public static LocalisableString DialogConfirmButton => OsuCcLocalisation.Get(getKey(nameof(DialogConfirmButton)), "Show confirmation dialog");

        public static LocalisableString DialogConfirmTitle => OsuCcLocalisation.Get(getKey(nameof(DialogConfirmTitle)), "Confirm test");

        public static LocalisableString DialogConfirmBody => OsuCcLocalisation.Get(getKey(nameof(DialogConfirmBody)), "This is a destructive-action confirmation, using the hold-to-confirm button.");

        public static LocalisableString DialogConfirmed => OsuCcLocalisation.Get(getKey(nameof(DialogConfirmed)), "confirmation dialog confirmed");

        public static LocalisableString DialogRestartButton => OsuCcLocalisation.Get(getKey(nameof(DialogRestartButton)), "Show restart dialog");

        public static LocalisableString DialogRestartTitle => OsuCcLocalisation.Get(getKey(nameof(DialogRestartTitle)), "Restart test");

        public static LocalisableString DialogRestartBody => OsuCcLocalisation.Get(getKey(nameof(DialogRestartBody)), "This is a non-destructive confirm for actions that need a restart.");

        public static LocalisableString DialogRestarted => OsuCcLocalisation.Get(getKey(nameof(DialogRestarted)), "restart dialog confirmed");

        public static LocalisableString DialogPushButton => OsuCcLocalisation.Get(getKey(nameof(DialogPushButton)), "Push a custom dialog");

        public static LocalisableString DialogPushTitle => OsuCcLocalisation.Get(getKey(nameof(DialogPushTitle)), "Custom dialog");

        public static LocalisableString DialogPushBody => OsuCcLocalisation.Get(getKey(nameof(DialogPushBody)), "This dialog was built inside the plugin and pushed with the generic host.Push.");

        public static LocalisableString DialogOk => OsuCcLocalisation.Get(getKey(nameof(DialogOk)), "OK");

        public static LocalisableString TitleLabel => OsuCcLocalisation.Get(getKey(nameof(TitleLabel)), "Title");

        public static LocalisableString SubtitleLabel => OsuCcLocalisation.Get(getKey(nameof(SubtitleLabel)), "Subtitle");

        public static LocalisableString AccentColourLabel => OsuCcLocalisation.Get(getKey(nameof(AccentColourLabel)), "Accent colour");

        public static LocalisableString BackgroundDimLabel => OsuCcLocalisation.Get(getKey(nameof(BackgroundDimLabel)), "Background dim");

        public static LocalisableString TotalDurationLabel => OsuCcLocalisation.Get(getKey(nameof(TotalDurationLabel)), "Total duration (ms)");

        public static LocalisableString ParticleDurationLabel => OsuCcLocalisation.Get(getKey(nameof(ParticleDurationLabel)), "Particle duration (ms)");

        public static LocalisableString ShowPersonalBestButton => OsuCcLocalisation.Get(getKey(nameof(ShowPersonalBestButton)), "Show personal best");
    }
}
