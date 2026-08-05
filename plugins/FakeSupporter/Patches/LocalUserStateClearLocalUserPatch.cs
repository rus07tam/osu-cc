using osucc.Core;
using osucc.Plugin;
using System;

namespace FakeSupporter
{
    /// <summary>
    /// Forgets the cached local user (and its id) when logging out, so the fake supporter stops
    /// matching new API responses until the next login.
    /// </summary>
    internal static class LocalUserStateClearLocalUserPatch
    {
        public static IDisposable? Install(IOsuCcPluginHost host)
            => PatchHelper.AttachPostfix(host, "osu.Game.Online.API.LocalUserState", "ClearLocalUser", typeof(LocalUserStateClearLocalUserPatch), nameof(Postfix));

        private static void Postfix()
        {
            SupporterFakerApi.Instance.OnLocalUserCleared();
        }
    }
}
