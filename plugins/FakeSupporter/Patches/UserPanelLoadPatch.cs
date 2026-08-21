using osu.Game.Users;
using osucc.Core;
using osucc.Plugin;

namespace FakeSupporter
{
    /// <summary>
    /// Registers every created <see cref="UserPanel"/> with the faker API.
    /// </summary>
    public sealed class UserPanelLoadPatch : PluginPatch<FakeSupporterPlugin>
    {
        public UserPanelLoadPatch(FakeSupporterPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Users.UserPanel", "load", MethodType.Postfix)
        {
        }

        public static void Postfix(UserPanel __instance)
        {
            SupporterFakerApi.Instance.OnPanelCreated(__instance);
        }
    }
}
