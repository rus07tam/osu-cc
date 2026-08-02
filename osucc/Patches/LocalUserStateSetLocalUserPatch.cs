using HarmonyLib;
using osu.Game.Online.API;
using osucc.Client;
using osucc.Core;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Fakes the logged-in user's supporter fields as soon as the real /me response is installed.
    /// The postfix runs after <c>LocalUserState.SetLocalUser</c> has written the game's own
    /// <c>configSupporter</c>, so <c>OsuSetting.WasSupporter</c> keeps the real value.
    /// </summary>
    public static class LocalUserStateSetLocalUserPatch
    {
        public static bool Install()
        {
            var method = Reflection.GetGameType("osu.Game.Online.API.LocalUserState")?.GetMethod("SetLocalUser", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            if (method == null)
            {
                TimingLog.Error("LocalUserStateSetLocalUserPatch: LocalUserState.SetLocalUser not found");
                return false;
            }

            HookDependencies.Create("dev.osucc.supporter.me").Patch(method, postfix: Reflection.HarmonyMethod(typeof(LocalUserStateSetLocalUserPatch), nameof(Postfix)));
            TimingLog.Info("LocalUserState.SetLocalUser patched (postfix)");
            return true;
        }

        private static void Postfix(LocalUserState __instance)
        {
            try
            {
                ClientSupporter.OnLocalUserSet(__instance.User);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"LocalUserStateSetLocalUserPatch.Postfix: {ex}");
            }
        }
    }
}
