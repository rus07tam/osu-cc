using osu.Game.Online.API;
using osucc.Core;
using osucc.Plugin;

namespace FakeSupporter
{
    /// <summary>
    /// Stamps users as fake supporters inside every API response. Targets the base
    /// <c>APIRequest.Perform()</c>.
    /// </summary>
    public sealed class APIRequestPerformPatch : PluginPatch<FakeSupporterPlugin>
    {
        public APIRequestPerformPatch(FakeSupporterPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Online.API.APIRequest", "Perform", MethodType.Postfix)
        {
        }

        public static void Postfix(APIRequest __instance)
        {
            var response = __instance.GetType().GetProperty("Response")?.GetValue(__instance);
            SupporterFakerApi.Instance.ApplyToResponse(response);
        }
    }
}
