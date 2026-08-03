using HarmonyLib;
using osu.Game.Overlays;
using osucc.Client;
using osucc.Core;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Captures the toolbar avatar button's <see cref="LoginOverlay"/> once it is resolved
    /// through dependency injection. The overlay is no longer owned by <c>OsuGame</c> in newer
    /// production builds (it lives on the main-menu screens), so it cannot be looked up by a
    /// game field; the button's <c>load</c> parameter is the exact instance the avatar opens.
    /// </summary>
    public static class ToolbarUserButtonLoadPatch
    {
        public static bool Install()
        {
            var load = Reflection.GetGameType("osu.Game.Overlays.Toolbar.ToolbarUserButton")
                                 ?.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                                 .FirstOrDefault(m => m.Name == "load" && m.GetParameters().Any(p => p.ParameterType.Name == "LoginOverlay"));
            if (load == null)
            {
                TimingLog.Error("ToolbarUserButtonLoadPatch: ToolbarUserButton.load(.. LoginOverlay) method not found");
                return false;
            }

            HookDependencies.Create("dev.osucc.supporter.login").Patch(load, postfix: Reflection.HarmonyMethod(typeof(ToolbarUserButtonLoadPatch), nameof(Postfix)));
            TimingLog.Info("ToolbarUserButton.load patched (postfix)");
            return true;
        }

        private static void Postfix(LoginOverlay login)
        {
            try
            {
                ClientSupporter.SetLoginOverlay(login);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"ToolbarUserButtonLoadPatch.Postfix: {ex}");
            }
        }
    }
}
