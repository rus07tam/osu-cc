using osu.Game.Online.API;
using osucc.Core;
using osucc.Plugin;

namespace FriendsLeaderboard
{
    /// <summary>
    /// Replaces the friend-scoped leaderboard request with a client-side aggregation of each
    /// friend's best score for the beatmap.
    /// </summary>
    public sealed class GetScoresRequestPatch : PluginPatch<FriendsLeaderboardPlugin>
    {
        public GetScoresRequestPatch(FriendsLeaderboardPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Online.API.APIRequest", "Perform", MethodType.Prefix)
        {
        }

        public override bool Condition => base.Condition && FriendsScoresAggregator.Enabled;

        public static bool Prefix(APIRequest __instance)
        {
            if (!FriendsScoresAggregator.ShouldIntercept(__instance))
                return true;

            FriendsScoresAggregator.BeginAggregation(__instance);
            return false;
        }
    }
}
