using osu.Game.Online.API;
using osucc.Core;
using osucc.Plugin;

namespace CustomUserGroups
{
    /// <summary>
    /// Stamps users' groups and colour inside every API response. Targets the base
    /// <c>APIRequest.Perform()</c>.
    /// </summary>
    public sealed class APIRequestPerformPatch : PluginPatch<CustomUserGroupsPlugin>
    {
        public APIRequestPerformPatch(CustomUserGroupsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, typeof(APIRequest), "Perform")
        {
        }

        public static void Postfix(APIRequest __instance)
        {
            var response = __instance.GetType().GetProperty("Response")?.GetValue(__instance);
            CustomUserGroupsApi.Instance.ApplyToResponse(response);
        }
    }
}
