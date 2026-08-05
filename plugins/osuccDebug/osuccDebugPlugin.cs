using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osucc.Plugin;

namespace osuccDebug
{
    /// <summary>
    /// The osu!cc debug plugin. Registers the debug toolbar button (right edge, first position)
    /// and the full-screen debug overlay through the plugin API, demonstrating blocking-overlay
    /// registration from a plugin.
    /// </summary>
    public class osuccDebugPlugin : IOsuCcPlugin, IOsuCcIconProvider
    {
        private IOsuCcPluginHost host = null!;
        private osuccDebugOverlay? overlay;

        /// <summary>The bug icon, matching the plugin's toolbar button.</summary>
        public IconUsage? Icon => FontAwesome.Solid.Bug;

        public void Load(IOsuCcPluginHost host)
        {
            this.host = host;

            // Negative layout position places it first in the right-hand group.
            host.AddToolbarButton(() => new osuccDebugButton(toggleOverlay), ToolbarButtonPlacement.Right, -1f);
        }

        public void AttachToGame()
        {
            overlay = new osuccDebugOverlay(host.Notify);
            host.RegisterBlockingOverlay(overlay);
        }

        private void toggleOverlay()
        {
            if (overlay == null)
                return;

            if (overlay.State.Value == Visibility.Hidden)
                overlay.Show();
            else
                overlay.Hide();
        }

        public void Dispose() => GC.SuppressFinalize(this);
    }
}
