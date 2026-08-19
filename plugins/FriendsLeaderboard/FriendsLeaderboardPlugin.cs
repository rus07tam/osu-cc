using osu.Framework.Bindables;
using osucc.Plugin;
using System;

namespace FriendsLeaderboard
{
    /// <summary>
    /// Shows the friend leaderboard (song select wedge and the beatmap set scores section)
    /// without an osu!supporter tag, by aggregating each friend's best score for the beatmap
    /// through the public per-user endpoint instead of the supporter-gated friend endpoint.
    /// </summary>
    public class FriendsLeaderboardPlugin : OsuCcPlugin
    {
        private PluginSettings settings = null!;

        protected override void OnLoad()
        {
            settings = Host.GetSettings();
            var enabled = settings.Bind("enabled", true);
            FriendsScoresAggregator.SetEnabledProvider(() => enabled.Value);
            FriendsScoresAggregator.SetHost(Host);

            Host.AddSettingsSubsection(() => new FriendsLeaderboardSettingsSubsection(settings));

            int patched = InstallPatches();
            Host.Log($"installed {patched}/2 patches");
            Host.Log("loaded");
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            settings?.Dispose();
            base.Dispose();
        }
    }
}
