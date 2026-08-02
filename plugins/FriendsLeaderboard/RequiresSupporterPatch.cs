using HarmonyLib;
using osu.Game.Screens.Play.Leaderboards;
using osucc.Core;

namespace FriendsLeaderboard
{
    /// <summary>
    /// Lifts the local osu!supporter gate for the friend leaderboard scope. The shared extension
    /// <c>ModelExtensions.RequiresSupporter</c> is checked by both <c>LeaderboardManager</c> and
    /// <c>ScoresContainer</c> before the request is created; the server-side gate is instead
    /// bypassed by <see cref="GetScoresRequestPatch"/>. All other scopes keep their original logic.
    /// </summary>
    internal static class RequiresSupporterPatch
    {
        public static bool Install(Harmony harmony)
            => PatchHelper.AttachPrefix(harmony, "osu.Game.Extensions.ModelExtensions", "RequiresSupporter", typeof(RequiresSupporterPatch), nameof(Prefix));

        private static bool Prefix(BeatmapLeaderboardScope scope, bool filterMods, ref bool __result)
        {
            if (scope != BeatmapLeaderboardScope.Friend || !FriendsScoresAggregator.Enabled)
                return true;

            __result = false;
            return false;
        }
    }
}
