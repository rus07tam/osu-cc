using HarmonyLib;
using osu.Game.Online.API;
using osu.Game.Online.Rooms;
using osucc.Client;
using osucc.Core;
using osucc.Localisation;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Blocks solo score submission when <see cref="ClientMods.DisableSoloScoreSubmission"/> is on.
    /// Targets <c>SoloPlayer.CreateTokenRequest()</c>: the prefix returns a null request, so the
    /// game's own token-retrieval handling treats it as "could not be constructed" — gameplay
    /// continues without a token and <c>SubmittingPlayer.submitScore</c> later skips submission.
    /// Only solo plays go through <c>SoloPlayer</c>; rooms/multiplayer (<c>RoomSubmittingPlayer</c>)
    /// are unaffected. Local scores are still saved by <c>Player.ImportScore</c>.
    /// </summary>
    public static class SoloScoreSubmissionPatch
    {
        public static bool Install()
        {
            var method = Reflection.GetGameType("osu.Game.Screens.Play.SoloPlayer")
                                   ?.GetMethod("CreateTokenRequest", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                TimingLog.Error("SoloScoreSubmissionPatch: SoloPlayer.CreateTokenRequest not found");
                return false;
            }

            HookDependencies.Create("dev.osucc.submission").Patch(method, prefix: Reflection.HarmonyMethod(typeof(SoloScoreSubmissionPatch), nameof(Prefix)));
            TimingLog.Info("SoloPlayer.CreateTokenRequest patched (prefix)");
            return true;
        }

        private static bool Prefix(ref APIRequest<APIScoreToken>? __result)
        {
            if (!ClientMods.DisableSoloScoreSubmission)
                return true;

            __result = null;
            ClientNotifications.Info(ModsStrings.ScoreSubmissionDisabled);
            return false;
        }
    }
}
