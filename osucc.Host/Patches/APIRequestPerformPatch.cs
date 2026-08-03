using HarmonyLib;
using osu.Game.Online.API;
using osucc.Client;
using osucc.Core;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Stamps the current user as a fake supporter inside every API response. Targets the base
    /// <c>APIRequest.Perform()</c> (non-generic, cannot be overridden) — by postfix time the
    /// deserialized <c>Response</c> is available on every <c>APIRequest&lt;T&gt;</c>, so
    /// leaderboards, scores, chat and user lookups all pick up the fake. The /me response is
    /// excluded (handled by <see cref="LocalUserStateSetLocalUserPatch"/>) so the game's own
    /// WasSupporter config write keeps the real value.
    /// </summary>
    public static class APIRequestPerformPatch
    {
        public static bool Install()
        {
            var perform = Reflection.GetGameType("osu.Game.Online.API.APIRequest")?.GetMethod("Perform", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            if (perform == null)
            {
                TimingLog.Error("APIRequestPerformPatch: APIRequest.Perform method not found");
                return false;
            }

            HookDependencies.Create("dev.osucc.supporter").Patch(perform, postfix: Reflection.HarmonyMethod(typeof(APIRequestPerformPatch), nameof(Postfix)));
            TimingLog.Info("APIRequest.Perform patched (postfix)");
            return true;
        }

        private static void Postfix(APIRequest __instance)
        {
            try
            {
                // Response is declared on the generic APIRequest<T>; read it reflectively.
                var response = __instance.GetType().GetProperty("Response")?.GetValue(__instance);
                ClientSupporter.ApplyToResponse(response);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"APIRequestPerformPatch.Postfix: {ex}");
            }
        }
    }
}
