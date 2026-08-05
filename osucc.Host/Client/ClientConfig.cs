using osu.Framework.Bindables;

namespace osucc.Client
{
    /// <summary>
    /// Strongly-held bindables for every <see cref="SpecialsSetting"/>, one per setting, bound
    /// two-way to <see cref="SpecialsConfigManager"/> on <see cref="Attach"/>. <see cref="SpecialsConfigManager.GetBindable{T}"/>
    /// returns weak copies that die once their last strong reference is gone, so every consumer
    /// (settings UI, client subsystems, patches) reads and writes the same live instances here
    /// instead of creating fresh weak copies per call.
    /// </summary>
    public static class ClientConfig
    {
        public static readonly Bindable<bool> Branding = new(true);
        public static readonly Bindable<bool> AllowIncompatibleMods = new(false);
        public static readonly Bindable<bool> ShowSystemMods = new(false);
        public static readonly Bindable<bool> FirstRunSetupComplete = new(false);
        public static readonly Bindable<bool> CelebrateNewRecord = new(true);
        public static readonly Bindable<bool> ShowRandomModsButton = new(true);
        public static readonly Bindable<bool> DisableSoloScoreSubmission = new(false);
        public static readonly Bindable<bool> SentryErrorReporting = new(false);
        public static readonly Bindable<bool> FavouriteMapHighlight = new(false);
        public static readonly Bindable<bool> ProfileFavouriteDownloadButton = new(false);

        /// <summary>Binds each strong bindable to the config's (weak) copy, syncing values both ways.</summary>
        public static void Attach(SpecialsConfigManager config)
        {
            bind(config, SpecialsSetting.Branding, Branding);
            bind(config, SpecialsSetting.AllowIncompatibleMods, AllowIncompatibleMods);
            bind(config, SpecialsSetting.ShowSystemMods, ShowSystemMods);
            bind(config, SpecialsSetting.FirstRunSetupComplete, FirstRunSetupComplete);
            bind(config, SpecialsSetting.CelebrateNewRecord, CelebrateNewRecord);
            bind(config, SpecialsSetting.ShowRandomModsButton, ShowRandomModsButton);
            bind(config, SpecialsSetting.DisableSoloScoreSubmission, DisableSoloScoreSubmission);
            bind(config, SpecialsSetting.SentryErrorReporting, SentryErrorReporting);
            bind(config, SpecialsSetting.FavouriteMapHighlight, FavouriteMapHighlight);
            bind(config, SpecialsSetting.ProfileFavouriteDownloadButton, ProfileFavouriteDownloadButton);
        }

        private static void bind<T>(SpecialsConfigManager config, SpecialsSetting setting, Bindable<T> strong)
            => config.BindWith(setting, strong);
    }
}
