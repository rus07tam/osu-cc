using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;
using osucc.Plugin;

namespace FriendsLeaderboard
{
    /// <summary>Settings subsection injected into the "Specials" section: the feature's master toggle.</summary>
    public partial class FriendsLeaderboardSettingsSubsection : SettingsSubsection
    {
        protected override LocalisableString Header => FriendsLeaderboardStrings.Name;

        public FriendsLeaderboardSettingsSubsection(PluginSettings settings)
        {
            this.AddCheckbox(settings, "enabled", true, FriendsLeaderboardStrings.EnableCaption, FriendsLeaderboardStrings.EnableHint);
        }
    }
}
