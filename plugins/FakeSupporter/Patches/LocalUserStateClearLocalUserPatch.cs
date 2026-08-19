using osucc.Core;

namespace FakeSupporter
{
    /// <summary>
    /// Forgets the cached local user (and its id) when logging out, so the fake supporter stops
    /// matching new API responses until the next login.
    /// </summary>
    [OsuCcPatch("osu.Game.Online.API.LocalUserState", "ClearLocalUser")]
    internal static class LocalUserStateClearLocalUserPatch
    {
        private static void Postfix()
        {
            SupporterFakerApi.Instance.OnLocalUserCleared();
        }
    }
}
