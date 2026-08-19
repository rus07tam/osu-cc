using osu.Game.Online.API;
using osucc.Core;
using osucc.Plugin;
using System;

namespace FakeSupporter
{
    /// <summary>
    /// Fakes the logged-in user's supporter fields as soon as the real /me response is installed.
    /// The postfix runs after <c>LocalUserState.SetLocalUser</c> has written the game's own
    /// <c>configSupporter</c>, so <c>OsuSetting.WasSupporter</c> keeps the real value.
    /// </summary>
    [OsuCcPatch("osu.Game.Online.API.LocalUserState", "SetLocalUser")]
    internal static class LocalUserStateSetLocalUserPatch
    {
        private static IOsuCcPluginHost host = null!;

        private static void Postfix(LocalUserState __instance)
        {
            try
            {
                SupporterFakerApi.Instance.OnLocalUserSet(__instance.User);
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"failed to fake local user: {ex}");
            }
        }
    }
}
