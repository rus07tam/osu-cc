using osu.Game.Online.API;
using osucc.Core;
using osucc.Plugin;
using System;

namespace CustomUserGroups
{
    /// <summary>Forgets the cached local user on logout (<c>LocalUserState.ClearLocalUser</c>).</summary>
    [OsuCcPatch("osu.Game.Online.API.LocalUserState", "ClearLocalUser")]
    internal static class LocalUserStateClearLocalUserPatch
    {
        private static IOsuCcPluginHost host = null!;

        private static void Postfix(LocalUserState __instance)
        {
            try
            {
                CustomUserGroupsApi.Instance.OnLocalUserCleared();
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"failed to clear cached local user: {ex}");
            }
        }
    }
}
