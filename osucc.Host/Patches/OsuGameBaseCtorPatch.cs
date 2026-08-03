using HarmonyLib;
using osucc.Client;
using osucc.Core;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Branding patch: postfixes the <c>OsuGameBase</c> parameterless ctor and overwrites its
    /// <c>Name</c> with the client branding (the window title reads it in
    /// <c>updateWindowTitle()</c>). Also proves the hook is in place before construction:
    /// <c>OsuGameBase</c> is constructed exactly once, in <c>Main()</c>, long before any
    /// ruleset injection could attach.
    /// </summary>
    public static class OsuGameBaseCtorPatch
    {
        public static bool Install()
        {
            var ctor = Reflection.GetGameType("osu.Game.OsuGameBase")?.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
            {
                TimingLog.Error("OsuGameBaseCtorPatch: parameterless ctor not found");
                return false;
            }

            HookDependencies.Main.Patch(ctor, postfix: Reflection.HarmonyMethod(typeof(OsuGameBaseCtorPatch), nameof(Postfix)));
            TimingLog.Info("OsuGameBase ctor patched (postfix)");
            return true;
        }

        private static void Postfix(object __instance)
        {
            try
            {
                ClientApi.CaptureOriginalGameName(Reflection.GetName(__instance as osu.Framework.Graphics.Drawable));
                Reflection.SetName(__instance as osu.Framework.Graphics.Drawable, ClientApi.BrandingName);
                TimingLog.Info($"Postfix: Name set to \"{ClientApi.BrandingName}\" on {__instance.GetType().FullName}");
            }
            catch (Exception ex)
            {
                TimingLog.Error($"Postfix: {ex}");
            }
        }
    }
}
