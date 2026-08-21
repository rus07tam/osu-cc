using osu.Game.Online.API;
using osucc.Core;
using osucc.Plugin;

namespace CustomUserGroups
{
    /// <summary>Forgets the cached local user on logout (<c>LocalUserState.ClearLocalUser</c>).</summary>
    public sealed class LocalUserStateClearLocalUserPatch : PluginPatch<CustomUserGroupsPlugin>
    {
        public LocalUserStateClearLocalUserPatch(CustomUserGroupsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Online.API.LocalUserState", "ClearLocalUser", MethodType.Postfix)
        {
        }

        public static void Postfix(LocalUserState __instance)
        {
            CustomUserGroupsApi.Instance.OnLocalUserCleared();
        }
    }
}
