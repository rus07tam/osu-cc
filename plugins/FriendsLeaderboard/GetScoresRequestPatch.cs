using osu.Game.Online.API;
using osucc.Core;
using osucc.Plugin;
using System;

namespace FriendsLeaderboard
{
    /// <summary>
    /// Replaces the friend-scoped leaderboard request with a client-side aggregation of each
    /// friend's best score for the beatmap. Runs as a prefix on the base <c>APIRequest.Perform()</c>
    /// (friend scope is not reachable through any overridable member), and is skipped for every
    /// other request so the normal API flow is untouched.
    /// </summary>
    internal static class GetScoresRequestPatch
    {
        public static IDisposable? Install(IOsuCcPluginHost host)
            => PatchHelper.AttachPrefix(host, "osu.Game.Online.API.APIRequest", "Perform", typeof(GetScoresRequestPatch), nameof(Prefix));

        private static bool Prefix(APIRequest __instance)
        {
            if (!FriendsScoresAggregator.ShouldIntercept(__instance))
                return true;

            FriendsScoresAggregator.BeginAggregation(__instance);
            return false;
        }
    }
}
