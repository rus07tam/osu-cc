using osu.Game.Overlays;
using osucc.Core;
using osucc.Plugin;
using System;

namespace FakeSupporter
{
    /// <summary>
    /// Captures the toolbar avatar button's <see cref="LoginOverlay"/> once it is resolved
    /// through dependency injection. The overlay is no longer owned by <c>OsuGame</c> in newer
    /// production builds (it lives on the main-menu screens), so it cannot be looked up by a
    /// game field; the button's <c>load</c> parameter is the exact instance the avatar opens.
    /// </summary>
    internal static class ToolbarUserButtonLoadPatch
    {
        public static IDisposable? Install(IOsuCcPluginHost host)
            => PatchHelper.AttachPostfix(host, "osu.Game.Overlays.Toolbar.ToolbarUserButton", "load", typeof(ToolbarUserButtonLoadPatch), nameof(Postfix));

        private static void Postfix(LoginOverlay login)
        {
            try
            {
                SupporterFakerApi.Instance.SetLoginOverlay(login);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"ToolbarUserButtonLoadPatch.Postfix: {ex}");
            }
        }
    }
}
