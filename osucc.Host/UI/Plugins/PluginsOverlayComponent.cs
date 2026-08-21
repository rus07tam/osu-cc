using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game;
using osucc.Core;
using osucc.Plugin;

namespace osucc.UI.Plugins
{
    /// <summary>
    /// Owns the <see cref="PluginsOverlay"/> and <see cref="PluginDetailsOverlay"/>, registered
    /// with the game's <see cref="osu.Game.Overlays.IOverlayManager"/> once the game has loaded.
    /// Exposes a static <see cref="Instance"/> so the Specials settings button and
    /// <see cref="PluginNameLink"/> can open the overlays without holding their own reference to
    /// the game.
    /// </summary>
    public partial class PluginsOverlayComponent : Container
    {
        public static PluginsOverlayComponent? Instance { get; private set; }

        private readonly PluginsOverlay overlay;
        private readonly PluginDetailsOverlay detailsOverlay;
        private readonly PluginDiagnosticsOverlay diagnosticsOverlay;
        private IDisposable? overlayRegistration;
        private IDisposable? detailsRegistration;
        private IDisposable? diagnosticsRegistration;

        [Resolved]
        private OsuGameBase game { get; set; } = null!;

        public PluginsOverlayComponent()
        {
            Instance = this;
            PluginNameLink.ShowDetailsHandler = ShowDetails;
            PluginNameLink.ShowDetailsEntryHandler = ShowDetails;
            overlay = new PluginsOverlay();
            detailsOverlay = new PluginDetailsOverlay();
            diagnosticsOverlay = new PluginDiagnosticsOverlay();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            overlayRegistration = Reflection.RegisterBlockingOverlay(game, overlay);
            detailsRegistration = Reflection.RegisterBlockingOverlay(game, detailsOverlay);
            diagnosticsRegistration = Reflection.RegisterBlockingOverlay(game, diagnosticsOverlay);
        }

        /// <summary>Toggles the plugins overlay.</summary>
        public void Toggle()
        {
            if (overlay.State.Value == Visibility.Hidden)
            {
                overlay.Show();
                TimingLog.Info("Plugins overlay shown");
            }
            else
            {
                overlay.Hide();
                TimingLog.Info("Plugins overlay hidden");
            }
        }

        /// <summary>Shows the details card of the given plugin, if it is loaded.</summary>
        public void ShowDetails(string pluginId)
        {
            var entry = PluginManager.Plugins.FirstOrDefault(p => p.Id == pluginId);

            if (entry == null)
                return;

            detailsOverlay.ShowPlugin(entry);
        }

        /// <summary>Shows the details card of the given plugin entry directly.</summary>
        public void ShowDetails(PluginEntry entry)
        {
            detailsOverlay.ShowPlugin(entry);
        }

        /// <summary>Shows the diagnostics overlay for the given plugin entry.</summary>
        public void ShowDiagnostics(PluginEntry entry)
        {
            diagnosticsOverlay.ShowPlugin(entry);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            overlayRegistration?.Dispose();
            detailsRegistration?.Dispose();
            diagnosticsRegistration?.Dispose();

            if (ReferenceEquals(Instance, this))
                Instance = null;
        }
    }
}
