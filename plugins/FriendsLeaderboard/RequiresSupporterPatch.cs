using osu.Game.Screens.Play.Leaderboards;
using osucc.Core;
using osucc.Plugin;

namespace FriendsLeaderboard
{
    /// <summary>
    /// Lifts the local osu!supporter gate for the friend leaderboard scope.
    /// </summary>
    public sealed class RequiresSupporterPatch : PluginPatch<FriendsLeaderboardPlugin>
    {
        public RequiresSupporterPatch(FriendsLeaderboardPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Extensions.ModelExtensions", "RequiresSupporter", MethodType.Prefix)
        {
        }

        public override bool Condition => base.Condition && FriendsScoresAggregator.Enabled;

        public static bool Prefix(BeatmapLeaderboardScope scope, bool filterMods, ref bool __result)
        {
            if (scope != BeatmapLeaderboardScope.Friend)
                return true;

            __result = false;
            return false;
        }
    }
}
