using osu.Game.Users;
using osucc.Core;
using osucc.Plugin;
using System;

namespace FakeSupporter
{
    /// <summary>
    /// Registers every created <see cref="UserPanel"/> (mini user card: friend list, online
    /// players, chat users, …) with the faker API. The panels snapshot their layout once in
    /// <c>load()</c> and never redraw it, so without this hook a per-user override applied later
    /// would not reach already-created cards; the API rebuilds them from its <c>Changed</c> event.
    /// </summary>
    internal static class UserPanelLoadPatch
    {
        public static IDisposable? Install(IOsuCcPluginHost host)
            => PatchHelper.AttachPostfix(host, "osu.Game.Users.UserPanel", "load", typeof(UserPanelLoadPatch), nameof(Postfix));

        private static void Postfix(UserPanel __instance)
        {
            try
            {
                SupporterFakerApi.Instance.OnPanelCreated(__instance);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"UserPanelLoadPatch.Postfix: {ex}");
            }
        }
    }
}
