using osu.Game.Online.API;
using osucc.Core;
using osucc.Plugin;
using System;

namespace FakeSupporter
{
    /// <summary>
    /// Stamps users as fake supporters inside every API response. Targets the base
    /// <c>APIRequest.Perform()</c> (non-generic, cannot be overridden) — by postfix time the
    /// deserialized <c>Response</c> is available on every <c>APIRequest&lt;T&gt;</c>, so
    /// leaderboards, scores, chat and user lookups all pick up the fake. The /me response is
    /// excluded (handled by <see cref="LocalUserStateSetLocalUserPatch"/>) so the game's own
    /// WasSupporter config write keeps the real value.
    /// </summary>
    internal static class APIRequestPerformPatch
    {
        public static IDisposable? Install(IOsuCcPluginHost host)
            => PatchHelper.AttachPostfix(host, "osu.Game.Online.API.APIRequest", "Perform", typeof(APIRequestPerformPatch), nameof(Postfix));

        private static void Postfix(APIRequest __instance)
        {
            try
            {
                // Response is declared on the generic APIRequest<T>; read it reflectively.
                var response = __instance.GetType().GetProperty("Response")?.GetValue(__instance);
                SupporterFakerApi.Instance.ApplyToResponse(response);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"APIRequestPerformPatch.Postfix: {ex}");
            }
        }
    }
}
