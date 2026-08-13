using osu.Game.Online.API;
using osucc.Core;
using osucc.Plugin;
using System;

namespace CustomUserGroups
{
    /// <summary>Forgets the cached local user on logout (<c>LocalUserState.ClearLocalUser</c>).</summary>
    internal static class LocalUserStateClearLocalUserPatch
    {
        public static IDisposable? Install(IOsuCcPluginHost host)
            => PatchHelper.AttachPostfix(host, "osu.Game.Online.API.LocalUserState", "ClearLocalUser", typeof(LocalUserStateClearLocalUserPatch), nameof(Postfix));

        private static void Postfix(LocalUserState __instance)
        {
            try
            {
                CustomUserGroupsApi.Instance.OnLocalUserCleared();
            }
            catch (Exception ex)
            {
                TimingLog.Error($"LocalUserStateClearLocalUserPatch.Postfix: {ex}");
            }
        }
    }
}
