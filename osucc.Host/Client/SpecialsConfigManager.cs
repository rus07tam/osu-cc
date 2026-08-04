using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace osucc.Client
{
    /// <summary>
    /// Idiomatic osu! config abstraction: an ini-backed <see cref="ConfigManager{TLookup}"/>
    /// exposing each <see cref="SpecialsSetting"/> as a bindable, persisted under the game's
    /// own storage ("osu-cc" folder). Settings UI and game logic bind to the same
    /// <see cref="osu.Framework.Bindables.IBindable{T}"/> instances.
    /// </summary>
    public class SpecialsConfigManager : IniConfigManager<SpecialsSetting>
    {
        /// <summary>
        /// The ini file holding the persisted settings, written under the game storage's
        /// "osu-cc" folder. Shared by the early Sentry-preference reader
        /// (<see cref="Core.SentryPreference"/>) so both read the same file.
        /// </summary>
        public const string ConfigFileName = "framework.ini";

        public SpecialsConfigManager(Storage storage)
            : base(storage)
        {
        }

        protected override string? Filename => ConfigFileName;

        protected override void InitialiseDefaults()
        {
            SetDefault(SpecialsSetting.Branding, true);
            SetDefault(SpecialsSetting.AllowIncompatibleMods, false);
            SetDefault(SpecialsSetting.ShowSystemMods, false);
            SetDefault(SpecialsSetting.FirstRunSetupComplete, false);
            SetDefault(SpecialsSetting.CelebrateNewRecord, true);
            SetDefault(SpecialsSetting.ShowRandomModsButton, true);
            SetDefault(SpecialsSetting.DisableSoloScoreSubmission, false);
            SetDefault(SpecialsSetting.SentryErrorReporting, false);
            SetDefault(SpecialsSetting.FakeSupporterEnabled, false);
            SetDefault(SpecialsSetting.FakeSupporterLevel, 2);
            SetDefault(SpecialsSetting.FavouriteMapHighlight, false);
            SetDefault(SpecialsSetting.ProfileFavouriteDownloadButton, false);
        }
    }
}
