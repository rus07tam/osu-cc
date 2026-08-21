using osu.Game.Users;
using osu.Game.Users.Drawables;
using osucc.Core;
using osucc.Plugin;

namespace SubdivideNations
{
    /// <summary>
    /// Swaps the country flag built by <c>UserPanel.CreateFlag()</c> for a <see cref="RegionUserFlag"/> composite.
    /// </summary>
    public sealed class UserPanelCreateFlagPatch : PluginPatch<SubdivideNationsPlugin>
    {
        public UserPanelCreateFlagPatch(SubdivideNationsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Users.UserPanel", "CreateFlag", MethodType.Postfix)
        {
        }

        public static void Postfix(UserPanel __instance, ref UpdateableFlag __result)
        {
            if (__result == null)
                return;

            __result = new RegionUserFlag(__instance.User, __result);
        }
    }
}
