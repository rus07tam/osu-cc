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
        private IDisposable? overlayRegistration;
        private IDisposable? detailsRegistration;

        [Resolved]
        private OsuGameBase game { get; set; } = null!;

        public PluginsOverlayComponent()
        {
            Instance = this;
            overlay = new PluginsOverlay();
            detailsOverlay = new PluginDetailsOverlay();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Schedule(registerOverlay);
            Schedule(registerDetailsOverlay);
        }

        private void registerOverlay()
        {
            overlayRegistration = Reflection.RegisterBlockingOverlay(game, overlay);

            if (overlayRegistration == null)
            {
                // overlayContent is only created inside OsuGame.load; retry until it exists.
                Schedule(registerOverlay);
                return;
            }

            TimingLog.Info("Plugins overlay registered via IOverlayManager");
        }

        private void registerDetailsOverlay()
        {
            detailsRegistration = Reflection.RegisterBlockingOverlay(game, detailsOverlay);

            if (detailsRegistration == null)
            {
                // overlayContent is only created inside OsuGame.load; retry until it exists.
                Schedule(registerDetailsOverlay);
                return;
            }

            TimingLog.Info("Plugin details overlay registered via IOverlayManager");
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

        /// <summary>
        /// Opens the plugins list overlay with its search box pre-filled with the given tag, so
        /// only the plugins carrying it are shown. Showing the list overlay hides any other
        /// osu!cc overlay (including the details card) via the overlay mutual exclusion.
        /// </summary>
        public void SearchTag(string tag)
        {
            overlay.SetFilter(tag);
            TimingLog.Info($"Plugins overlay shown with tag filter '{tag}'");
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            overlayRegistration?.Dispose();
            detailsRegistration?.Dispose();

            if (ReferenceEquals(Instance, this))
                Instance = null;
        }
    }
}
