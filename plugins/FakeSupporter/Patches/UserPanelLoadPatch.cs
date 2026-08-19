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
    [OsuCcPatch("osu.Game.Users.UserPanel", "load")]
    internal static class UserPanelLoadPatch
    {
        private static IOsuCcPluginHost host = null!;

        private static void Postfix(UserPanel __instance)
        {
            try
            {
                SupporterFakerApi.Instance.OnPanelCreated(__instance);
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"failed to handle created user panel: {ex}");
            }
        }
    }
}
