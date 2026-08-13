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
        /// (<see cref="Core.SentryPreference"/>) so both read the same file. Matches the
        /// framework's <c>IniConfigManager</c> default (renamed from "framework.ini" to
        /// "game.ini" in recent osu.Framework versions).
        /// </summary>
        public const string ConfigFileName = "game.ini";

        public SpecialsConfigManager(Storage storage)
            : base(storage)
        {
        }

        protected override string? Filename => ConfigFileName;

        protected override void InitialiseDefaults()
        {
            SetDefault(SpecialsSetting.Branding, true);
            SetDefault(SpecialsSetting.AllowIncompatibleMods, true);
            SetDefault(SpecialsSetting.ShowSystemMods, true);
            SetDefault(SpecialsSetting.FirstRunSetupComplete, false);
            SetDefault(SpecialsSetting.CelebrateNewRecord, true);
            SetDefault(SpecialsSetting.ShowRandomModsButton, true);
            SetDefault(SpecialsSetting.DisableSoloScoreSubmission, false);
            SetDefault(SpecialsSetting.SentryErrorReporting, false);
            SetDefault(SpecialsSetting.FavouriteMapHighlight, true);
            SetDefault(SpecialsSetting.ProfileFavouriteDownloadButton, true);
            SetDefault(SpecialsSetting.OsuCcTheme, Core.OsuCcThemeRegistry.DefaultId);
        }
    }
}
