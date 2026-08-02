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
        public SpecialsConfigManager(Storage storage)
            : base(storage)
        {
        }

        protected override void InitialiseDefaults()
        {
            SetDefault(SpecialsSetting.Branding, true);
            SetDefault(SpecialsSetting.AllowIncompatibleMods, false);
            SetDefault(SpecialsSetting.ShowSystemMods, false);
            SetDefault(SpecialsSetting.FirstRunSetupComplete, false);
            SetDefault(SpecialsSetting.CelebrateNewRecord, true);
            SetDefault(SpecialsSetting.RandomModsButton, true);
            SetDefault(SpecialsSetting.DisableSoloScoreSubmission, false);
            SetDefault(SpecialsSetting.SentryErrorReporting, false);
            SetDefault(SpecialsSetting.FakeSupporterEnabled, false);
            SetDefault(SpecialsSetting.FakeSupporterLevel, 2);
            SetDefault(SpecialsSetting.FavouriteMapHighlight, false);
            SetDefault(SpecialsSetting.ProfileFavouriteDownloadButton, false);
        }
    }
}
