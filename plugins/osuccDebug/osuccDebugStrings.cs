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

        public static LocalisableString PersonalBestPanelTitle => OsuCcLocalisation.Get(getKey(nameof(PersonalBestPanelTitle)), "Personal best");

        public static LocalisableString NotificationMessageLabel => OsuCcLocalisation.Get(getKey(nameof(NotificationMessageLabel)), "Notification message");

        public static LocalisableString NotificationKindLabel => OsuCcLocalisation.Get(getKey(nameof(NotificationKindLabel)), "Notification kind");

        public static LocalisableString PostNotificationButton => OsuCcLocalisation.Get(getKey(nameof(PostNotificationButton)), "Post notification");

        public static LocalisableString TitleLabel => OsuCcLocalisation.Get(getKey(nameof(TitleLabel)), "Title");

        public static LocalisableString SubtitleLabel => OsuCcLocalisation.Get(getKey(nameof(SubtitleLabel)), "Subtitle");

        public static LocalisableString AccentColourLabel => OsuCcLocalisation.Get(getKey(nameof(AccentColourLabel)), "Accent colour");

        public static LocalisableString BackgroundDimLabel => OsuCcLocalisation.Get(getKey(nameof(BackgroundDimLabel)), "Background dim");

        public static LocalisableString TotalDurationLabel => OsuCcLocalisation.Get(getKey(nameof(TotalDurationLabel)), "Total duration (ms)");

        public static LocalisableString ParticleDurationLabel => OsuCcLocalisation.Get(getKey(nameof(ParticleDurationLabel)), "Particle duration (ms)");

        public static LocalisableString ShowPersonalBestButton => OsuCcLocalisation.Get(getKey(nameof(ShowPersonalBestButton)), "Show personal best");
    }
}
