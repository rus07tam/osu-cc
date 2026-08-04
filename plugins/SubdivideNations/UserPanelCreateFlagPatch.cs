using osu.Game.Users;
using osu.Game.Users.Drawables;
using osucc.Core;
using osucc.Plugin;
using System;

namespace SubdivideNations
{
    /// <summary>
    /// Swaps the country flag built by <c>UserPanel.CreateFlag()</c> for a <see cref="RegionUserFlag"/>
    /// composite, adding a region badge to every panel that renders through the shared method
    /// (rank, list, grid and online panels — including the toolbar mini-card).
    /// </summary>
    internal static class UserPanelCreateFlagPatch
    {
        public static IDisposable? Install(IOsuCcPluginHost host)
            => PatchHelper.AttachPostfix(host, "osu.Game.Users.UserPanel", "CreateFlag", typeof(UserPanelCreateFlagPatch), nameof(Postfix));

        private static void Postfix(UserPanel __instance, ref UpdateableFlag __result)
        {
            if (__result == null)
                return;

            __result = new RegionUserFlag(__instance.User, __result);
        }
    }
}
