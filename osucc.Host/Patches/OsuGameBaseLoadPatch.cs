using HarmonyLib;
using osu.Game;
using osucc.Client;
using osucc.Core;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Wires the client into the game once it is constructed and loading. Targets the private
    /// <c>[BackgroundDependencyLoader]</c> <c>load(...)</c> on <c>OsuGameBase</c> — the first
    /// point where the game instance, storage and dependency injection are all available. The
    /// postfix hands the instance to <see cref="ClientBootstrap"/>.
    /// </summary>
    public static class OsuGameBaseLoadPatch
    {
        public static bool Install()
        {
            var load = Reflection.GetGameType("osu.Game.OsuGameBase")
                                 ?.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                                 .FirstOrDefault(m => m.Name == "load" && m.GetParameters().Length == 2);
            if (load == null)
            {
                TimingLog.Error("OsuGameBaseLoadPatch: load(..) method not found");
                return false;
            }

            HookDependencies.Create("dev.osucc.load").Patch(load, postfix: Reflection.HarmonyMethod(typeof(OsuGameBaseLoadPatch), nameof(Postfix)));
            TimingLog.Info("OsuGameBase.load patched (postfix)");
            return true;
        }

        private static void Postfix(OsuGameBase __instance)
        {
            try
            {
                ClientBootstrap.AttachToGame(__instance);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"OsuGameBaseLoadPatch.Postfix: {ex}");
            }
        }
    }
}
