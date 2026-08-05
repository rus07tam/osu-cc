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
    public class FriendsLeaderboardPlugin : OsuCcPluginBase
    {
        private PluginSettings settings = null!;
        private IDisposable? supporterGatePatch;
        private IDisposable? requestPatch;

        protected override void OnLoad()
        {
            settings = Host.GetSettings();
            var enabled = settings.Bind("enabled", true);
            FriendsScoresAggregator.SetEnabledProvider(() => enabled.Value);

            Host.AddSettingsSubsection(() => new FriendsLeaderboardSettingsSubsection(settings));

            supporterGatePatch = RequiresSupporterPatch.Install(Host);
            requestPatch = GetScoresRequestPatch.Install(Host);

            Host.Log($"patches: supporter-gate={supporterGatePatch != null}, request={requestPatch != null}");
            Host.Log("loaded");
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            supporterGatePatch?.Dispose();
            requestPatch?.Dispose();
            settings?.Dispose();
            base.Dispose();
        }
    }
}
