namespace osucc.Client
{
    /// <summary>Client-specific settings, persisted by <see cref="SpecialsConfigManager"/>.</summary>
    public enum SpecialsSetting
    {
        Branding,

        /// <summary>Allow selecting/playing mods that are normally incompatible with each-other.</summary>
        AllowIncompatibleMods,

        /// <summary>Show the ModType.System column (score v2, touch device, ...) in the mod selector.</summary>
        ShowSystemMods,

        /// <summary>Whether the first-run setup (disclaimer, onboarding) has been completed. Auto-set to true after the first run.</summary>
        FirstRunSetupComplete,

        /// <summary>Show a full-screen particle celebration when a play sets a new local personal best on a beatmap.</summary>
        CelebrateNewRecord,

        /// <summary>Show a "Random mods" button in the mod-select overlay footer.</summary>
        RandomModsButton,

        /// <summary>Block solo score submission to the osu! servers. Local scores are still saved.</summary>
        DisableSoloScoreSubmission,

        /// <summary>Send error reports (Sentry) to osu servers. Applied on the next launch.</summary>
        SentryErrorReporting,

        /// <summary>Visually fake the current player's osu!supporter tag everywhere. Local cosmetic only: no server interaction.</summary>
        FakeSupporterEnabled,

        /// <summary>The faked supporter level (1–10 hearts). Only meaningful when <see cref="FakeSupporterEnabled"/> is on.</summary>
        FakeSupporterLevel,

        /// <summary>Draw a pink pulsing outline with particles around favourited beatmaps in the song select carousel.</summary>
        FavouriteMapHighlight,

        /// <summary>Add a "download all favourites" button to the Beatmaps → Favourites section of user profiles.</summary>
        ProfileFavouriteDownloadButton
    }
}
