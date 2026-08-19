using osu.Game.Online.API;
using osucc.Core;
using osucc.Plugin;
using System;

namespace CustomUserGroups
{
    /// <summary>
    /// Stamps the logged-in user's groups and colour as soon as the real /me response is installed,
    /// so own-profile and user cards reflect any custom group assigned to the current player.
    /// </summary>
    internal static class LocalUserStateSetLocalUserPatch
    {
        private static IOsuCcPluginHost host = null!;

        public static IDisposable? Install(IOsuCcPluginHost host)
        {
            LocalUserStateSetLocalUserPatch.host = host;
            return PatchHelper.AttachPostfix(host, "osu.Game.Online.API.LocalUserState", "SetLocalUser", typeof(LocalUserStateSetLocalUserPatch), nameof(Postfix));
        }

        private static void Postfix(LocalUserState __instance)
        {
            try
            {
                CustomUserGroupsApi.Instance.OnLocalUserSet(__instance.User);
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"failed to stamp local user: {ex}");
            }
        }
    }
}
