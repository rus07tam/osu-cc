using osu.Game.Database;
using osu.Game.Models;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osucc.Celebrations;
using osucc.Client;
using osucc.Core;
using osucc.Localisation;
using Realms;
using System.Linq;

namespace osucc.Patches
{
    /// <summary>
    /// Posts a "new personal best" celebration when a completed play's total score exceeds the
    /// previous best local score for the same beatmap+ruleset.
    /// Targets <c>Player.ImportScore(Score)</c>.
    /// </summary>
    public sealed class PlayerImportScorePatch : OsuCcPatch
    {
        public PlayerImportScorePatch()
            : base("osu.Game.Screens.Play.Player", "ImportScore", MethodType.Postfix)
        {
        }

        public override bool Condition => ClientMods.CelebrateNewRecord;

        public static bool Prefix(Player __instance, Score score, ref long? __state)
        {
            __state = null;

            if (__instance is not SubmittingPlayer)
                return true;

            if (!__instance.GameplayState.HasPassed)
                return true;

            if (score.ScoreInfo.User.IsBot)
                return true;

            var realm = ClientApi.Game?.Dependencies?.Get(typeof(RealmAccess)) as RealmAccess;
            if (realm == null)
                return true;

            string beatmapHash = score.ScoreInfo.BeatmapHash;
            if (string.IsNullOrEmpty(beatmapHash))
                return true;

            int userId = score.ScoreInfo.UserID;

            long? previousBest = realm.Run(r => r.All<ScoreInfo>()
                                                  .Filter($@"({nameof(ScoreInfo.User)}.{nameof(RealmUser.OnlineID)} == $0 || {nameof(ScoreInfo.User)}.{nameof(RealmUser.OnlineID)} <= 1)" +
                                                          $" && {nameof(ScoreInfo.BeatmapHash)} == $1" +
                                                          $" && {nameof(ScoreInfo.Ruleset)}.{nameof(RulesetInfo.OnlineID)} == $2",
                                                      userId, beatmapHash, score.ScoreInfo.Ruleset.OnlineID)
                                                  .OrderByDescending(s => s.TotalScore)
                                                  .FirstOrDefault()?.TotalScore);

            if (previousBest == null || score.ScoreInfo.TotalScore > previousBest.Value)
                __state = score.ScoreInfo.TotalScore;

            return true;
        }

        public static void Postfix(Player __instance, Score score, ref long? __state)
        {
            if (__state == null)
                return;

            long totalScore = __state.Value;
            ClientCelebrations.Show(new BestScoreCelebration(PersonalBestStrings.Title, totalScore));
        }
    }
}
