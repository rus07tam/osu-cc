using HarmonyLib;
using osucc.Client;
using osucc.Core;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Forgets the cached local user (and its id) when logging out, so the fake supporter stops
    /// matching new API responses until the next login.
    /// </summary>
    public static class LocalUserStateClearLocalUserPatch
    {
        public static bool Install()
        {
            var method = Reflection.GetGameType("osu.Game.Online.API.LocalUserState")?.GetMethod("ClearLocalUser", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            if (method == null)
            {
                TimingLog.Error("LocalUserStateClearLocalUserPatch: LocalUserState.ClearLocalUser not found");
                return false;
            }

            HookDependencies.Main.Patch(method, postfix: Reflection.HarmonyMethod(typeof(LocalUserStateClearLocalUserPatch), nameof(Postfix)));
            TimingLog.Info("LocalUserState.ClearLocalUser patched (postfix)");
            return true;
        }

        private static void Postfix()
        {
            ClientSupporter.OnLocalUserCleared();
        }
    }
}
