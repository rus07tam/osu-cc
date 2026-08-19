using osu.Game.Users;
using osu.Game.Users.Drawables;
using osucc.Core;

namespace SubdivideNations
{
    /// <summary>
    /// Swaps the country flag built by <c>UserPanel.CreateFlag()</c> for a <see cref="RegionUserFlag"/>
    /// composite, adding a region badge to every panel that renders through the shared method
    /// (rank, list, grid and online panels — including the toolbar mini-card).
    /// </summary>
    [OsuCcPatch("osu.Game.Users.UserPanel", "CreateFlag")]
    internal static class UserPanelCreateFlagPatch
    {

        private static void Postfix(UserPanel __instance, ref UpdateableFlag __result)
        {
            if (__result == null)
                return;

            __result = new RegionUserFlag(__instance.User, __result);
        }
    }
}
