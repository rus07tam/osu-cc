using osu.Game.Overlays;
using osucc.Core;
using osucc.Plugin;

namespace FakeSupporter
{
    /// <summary>
    /// Captures the toolbar avatar button's <see cref="LoginOverlay"/> once it is resolved.
    /// </summary>
    public sealed class ToolbarUserButtonLoadPatch : PluginPatch<FakeSupporterPlugin>
    {
        public ToolbarUserButtonLoadPatch(FakeSupporterPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Overlays.Toolbar.ToolbarUserButton", "load", MethodType.Postfix)
        {
        }

        public static void Postfix(LoginOverlay login)
        {
            SupporterFakerApi.Instance.SetLoginOverlay(login);
        }
    }
}
