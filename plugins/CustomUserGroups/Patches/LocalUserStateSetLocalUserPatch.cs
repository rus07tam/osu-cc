using osu.Game.Online.API;
using osucc.Core;
using osucc.Plugin;

namespace CustomUserGroups
{
    /// <summary>
    /// Stamps the logged-in user's groups and colour as soon as the real /me response is installed.
    /// </summary>
    public sealed class LocalUserStateSetLocalUserPatch : PluginPatch<CustomUserGroupsPlugin>
    {
        public LocalUserStateSetLocalUserPatch(CustomUserGroupsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Online.API.LocalUserState", "SetLocalUser", MethodType.Postfix)
        {
        }

        public static void Postfix(LocalUserState __instance)
        {
            CustomUserGroupsApi.Instance.OnLocalUserSet(__instance.User);
        }
    }
}
