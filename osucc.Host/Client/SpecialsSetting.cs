namespace osucc.Client
{
    /// <summary>Client-specific settings, persisted by <see cref="SpecialsConfigManager"/>.</summary>
    public enum SpecialsSetting
    {
        Branding,

        /// <summary>Show a SKIP button during mid-map break periods.</summary>
        SkipBreakTime,

        /// <summary>Allow selecting/playing mods that are normally incompatible with each-other.</summary>
        AllowIncompatibleMods,

        /// <summary>Show the ModType.System column (score v2, touch device, ...) in the mod selector.</summary>
        ShowSystemMods,

        /// <summary>Whether the first-run setup (disclaimer, onboarding) has been completed. Auto-set to true after the first run.</summary>
        FirstRunSetupComplete,

        /// <summary>Show a full-screen particle celebration when a play sets a new local personal best on a beatmap.</summary>
        CelebrateNewRecord,

        /// <summary>Show a "Random mods" button in the mod-select overlay footer.</summary>
        ShowRandomModsButton,

        /// <summary>Block solo score submission to the osu! servers. Local scores are still saved.</summary>
        DisableSoloScoreSubmission,

        /// <summary>Send error reports (Sentry) to osu servers. Applied before <c>SentryLogger</c> is constructed on the next launch.</summary>
        SentryErrorReporting,

        /// <summary>Draw a pink pulsing outline with particles around favourited beatmaps in the song select carousel.</summary>
        FavouriteMapHighlight,

        /// <summary>Add a "download all favourites" button to the Beatmaps → Favourites section of user profiles.</summary>
        ProfileFavouriteDownloadButton,

        /// <summary>Cosmetic UI palette applied to the client chrome and osu-cc surfaces, stored as an <see cref="Core.OsuCcThemeRegistry"/> theme id. Restart-gated.</summary>
        OsuCcTheme,

        /// <summary>Position of the key history overlay, or Disabled if off.</summary>
        KeyHistoryMode,

        /// <summary>Experimental: enable or disable plugins at runtime without restarting the game. Restart-gated.</summary>
        LivePluginReloading,

        /// <summary>Experimental: bypass osu!cc and osu!lazer version compatibility checks in plugin dependencies.</summary>
        BypassHostDependencyCheck,

        /// <summary>Experimental: bypass inter-plugin dependency version compatibility checks and warnings.</summary>
        BypassPluginDependencyCheck
    }
}
