using osu.Framework.Localisation;

namespace osucc.Localisation
{
    public static class SpecialsSettingsStrings
    {
        private const string prefix = "osucc.Localisation.Specials";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString SectionHeader => OsuCcLocalisation.Get(getKey(nameof(SectionHeader)), "Specials");

        public static LocalisableString SubsectionHeader => OsuCcLocalisation.Get(getKey(nameof(SubsectionHeader)), "osu!cc");

        public static LocalisableString BrandingCaption => OsuCcLocalisation.Get(getKey(nameof(BrandingCaption)), "Branding (window title)");

        public static LocalisableString AllowIncompatibleModsCaption => OsuCcLocalisation.Get(getKey(nameof(AllowIncompatibleModsCaption)), "Allow incompatible mods");

        public static LocalisableString AllowIncompatibleModsHint => OsuCcLocalisation.Get(getKey(nameof(AllowIncompatibleModsHint)), "Permits selecting mods that are normally incompatible with each-other (e.g. autoplay + touch device). The red 'incompatible' hint on mod panels is kept as a visual warning.");

        public static LocalisableString ShowSystemModsCaption => OsuCcLocalisation.Get(getKey(nameof(ShowSystemModsCaption)), "Show System mods in mod selector");

        public static LocalisableString ShowSystemModsHint => OsuCcLocalisation.Get(getKey(nameof(ShowSystemModsHint)), "Displays the 'System' column (score v2, touch device, ...) in the mod selector overlay.");

        public static LocalisableString FirstRunSetupCompleteCaption => OsuCcLocalisation.Get(getKey(nameof(FirstRunSetupCompleteCaption)), "First-run setup complete");

        public static LocalisableString FirstRunSetupCompleteHint => OsuCcLocalisation.Get(getKey(nameof(FirstRunSetupCompleteHint)), "Whether the first-run disclaimer has been shown and acknowledged. Auto-enabled after the disclaimer is dismissed; uncheck to show it again on next launch.");

        public static LocalisableString CelebrateNewRecordCaption => OsuCcLocalisation.Get(getKey(nameof(CelebrateNewRecordCaption)), "Celebrate on new personal best");

        public static LocalisableString CelebrateNewRecordHint => OsuCcLocalisation.Get(getKey(nameof(CelebrateNewRecordHint)), "Shows a full-screen particle celebration when a completed play sets a new local personal best on a beatmap.");

        public static LocalisableString DisableSoloScoreSubmissionCaption => OsuCcLocalisation.Get(getKey(nameof(DisableSoloScoreSubmissionCaption)), "Disable score submission");

        public static LocalisableString DisableSoloScoreSubmissionHint => OsuCcLocalisation.Get(getKey(nameof(DisableSoloScoreSubmissionHint)), "Blocks submission of solo scores to the osu! servers. Local scores are still saved; a reminder is shown each time a play starts.");

        public static LocalisableString RandomModsButtonCaption => OsuCcLocalisation.Get(getKey(nameof(RandomModsButtonCaption)), "Random mods button");

        public static LocalisableString RandomModsButtonHint => OsuCcLocalisation.Get(getKey(nameof(RandomModsButtonHint)), "Adds a 'Random mods' button to the mod-select overlay footer that picks a random set of mods.");

        public static LocalisableString SentryErrorReportingCaption => OsuCcLocalisation.Get(getKey(nameof(SentryErrorReportingCaption)), "Send error reports to osu (Sentry)");

        public static LocalisableString SentryErrorReportingHint => OsuCcLocalisation.Get(getKey(nameof(SentryErrorReportingHint)), "Whether the game may send anonymous error reports to the osu! servers. Disabled by default to avoid leaking client-specific patterns. Applied before the error logger starts on the next launch.");

        public static LocalisableString FavouriteMapHighlightCaption => OsuCcLocalisation.Get(getKey(nameof(FavouriteMapHighlightCaption)), "Highlight favourited maps in song select");

        public static LocalisableString FavouriteMapHighlightHint => OsuCcLocalisation.Get(getKey(nameof(FavouriteMapHighlightHint)), "Draws a pink pulsing outline with drifting particles around beatmaps you have favourited, directly in the song select carousel.");

        public static LocalisableString ProfileFavouriteDownloadButtonCaption => OsuCcLocalisation.Get(getKey(nameof(ProfileFavouriteDownloadButtonCaption)), "Download all favourites button (profile)");

        public static LocalisableString ProfileFavouriteDownloadButtonHint => OsuCcLocalisation.Get(getKey(nameof(ProfileFavouriteDownloadButtonHint)), "Adds a 'Download all favourites' button to the Beatmaps → Favourites section of user profiles.");

        public static LocalisableString ManagePluginsCaption => OsuCcLocalisation.Get(getKey(nameof(ManagePluginsCaption)), "Manage plugins…");

        public static LocalisableString ManagePluginsTooltip => OsuCcLocalisation.Get(getKey(nameof(ManagePluginsTooltip)), "Opens the osu!cc plugins overlay, listing every loaded plugin and its status.");
    }
}
