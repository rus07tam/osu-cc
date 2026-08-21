using osu.Game.Online.API;
using osucc.Core;
using osucc.Plugin;

namespace FakeSupporter
{
    /// <summary>
    /// Fakes the logged-in user's supporter fields as soon as the real /me response is installed.
    /// </summary>
    public sealed class LocalUserStateSetLocalUserPatch : PluginPatch<FakeSupporterPlugin>
    {
        public LocalUserStateSetLocalUserPatch(FakeSupporterPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Online.API.LocalUserState", "SetLocalUser", MethodType.Postfix)
        {
        }

        public static void Postfix(LocalUserState __instance)
        {
            SupporterFakerApi.Instance.OnLocalUserSet(__instance.User);
        }
    }
}
