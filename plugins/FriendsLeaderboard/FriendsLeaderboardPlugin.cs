using osu.Framework.Bindables;
using osucc.Plugin;

namespace FriendsLeaderboard
{
    /// <summary>
    /// Shows the friend leaderboard (song select wedge and the beatmap set scores section)
    /// without an osu!supporter tag, by aggregating each friend's best score for the beatmap
    /// through the public per-user endpoint instead of the supporter-gated friend endpoint.
    /// </summary>
    [OsuCcPlugin(
        "friends-leaderboard",
        "Friends Leaderboard",
        Author = "osu-cc",
        Description = "Shows the friend leaderboard without an osu!supporter tag by aggregating each friend's best score client-side.",
        Version = "1.0.0")]
    public class FriendsLeaderboardPlugin : OsuCcPluginBase
    {
        private PluginSettings settings = null!;

        protected override void OnLoad()
        {
            settings = Host.GetSettings();
            var enabled = settings.Bind("enabled", true);
            FriendsScoresAggregator.SetEnabledProvider(() => enabled.Value);

            Host.AddSettingsSubsection(() => new FriendsLeaderboardSettingsSubsection(settings));

            var harmony = Host.CreateHarmony("friends-leaderboard");

            bool gatePatched = RequiresSupporterPatch.Install(harmony);
            bool requestPatched = GetScoresRequestPatch.Install(harmony);

            Host.Log($"patches: supporter-gate={gatePatched}, request={requestPatched}");
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
