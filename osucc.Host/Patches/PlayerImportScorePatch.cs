using HarmonyLib;
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
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Posts a "new personal best" celebration when a completed play's total score exceeds the
    /// previous best local score for the same beatmap+ruleset.
    /// Targets <c>Player.ImportScore(Score)</c> (protected virtual). ReplayPlayer overrides
    /// <c>ImportScore</c> without calling base, so replay viewing never triggers this.
    /// </summary>
    public static class PlayerImportScorePatch
    {
        public static bool Install()
        {
            var method = Reflection.GetGameType("osu.Game.Screens.Play.Player")?.GetMethod("ImportScore", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                TimingLog.Error("PlayerImportScorePatch: ImportScore method not found");
                return false;
            }

            HookDependencies.Main
                            .Patch(method,
                                prefix: Reflection.HarmonyMethod(typeof(PlayerImportScorePatch), nameof(Prefix)),
                                postfix: Reflection.HarmonyMethod(typeof(PlayerImportScorePatch), nameof(Postfix)));
            TimingLog.Info("Player.ImportScore patched (prefix+postfix)");
            return true;
        }

        private static bool Prefix(Player __instance, Score score, ref long? __state)
        {
            __state = null;

            try
            {
                if (__instance is not SubmittingPlayer)
                    return true;

                if (!__instance.GameplayState.HasPassed)
                    return true;

                if (score.ScoreInfo.User.IsBot)
                    return true;

                if (!ClientMods.CelebrateNewRecord)
                    return true;

                var realm = ClientApi.Game?.Dependencies?.Get(typeof(RealmAccess)) as RealmAccess;
                if (realm == null)
                    return true;

                string beatmapHash = score.ScoreInfo.BeatmapHash;
                if (string.IsNullOrEmpty(beatmapHash))
                    return true;

                // Mirror the game's own "scores belonging to this user" semantics
                // (see ScoreInfoExtensions.GetAllLocalScoresForUser): the user is either
                // the current one, or a guest/system score (OnlineID <= 1).
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
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PlayerImportScorePatch.Prefix: {ex}");
            }

            return true;
        }

        private static void Postfix(Player __instance, Score score, ref long? __state)
        {
            if (__state == null)
                return;

            try
            {
                long totalScore = __state.Value;
                ClientCelebrations.Show(new BestScoreCelebration(PersonalBestStrings.Title, totalScore));
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PlayerImportScorePatch.Postfix: {ex}");
            }
        }
    }
}
