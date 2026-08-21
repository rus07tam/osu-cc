using osu.Game.Online.API;
using osu.Game.Online.Rooms;
using osucc.Client;
using osucc.Core;
using osucc.Localisation;

namespace osucc.Patches
{
    /// <summary>
    /// Blocks solo score submission when <see cref="ClientMods.DisableSoloScoreSubmission"/> is on.
    /// Targets <c>SoloPlayer.CreateTokenRequest()</c>.
    /// </summary>
    public sealed class SoloScoreSubmissionPatch : OsuCcPatch
    {
        public SoloScoreSubmissionPatch()
            : base("osu.Game.Screens.Play.SoloPlayer", "CreateTokenRequest", MethodType.Prefix)
        {
        }

        public override bool Condition => ClientMods.DisableSoloScoreSubmission;

        public static bool Prefix(ref APIRequest<APIScoreToken>? __result)
        {
            __result = null;
            ClientNotifications.Info(ModsStrings.ScoreSubmissionDisabled);
            return false;
        }
    }
}
