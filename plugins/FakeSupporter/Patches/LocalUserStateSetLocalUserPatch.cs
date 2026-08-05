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
    internal static class LocalUserStateSetLocalUserPatch
    {
        public static IDisposable? Install(IOsuCcPluginHost host)
            => PatchHelper.AttachPostfix(host, "osu.Game.Online.API.LocalUserState", "SetLocalUser", typeof(LocalUserStateSetLocalUserPatch), nameof(Postfix));

        private static void Postfix(LocalUserState __instance)
        {
            try
            {
                SupporterFakerApi.Instance.OnLocalUserSet(__instance.User);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"LocalUserStateSetLocalUserPatch.Postfix: {ex}");
            }
        }
    }
}
