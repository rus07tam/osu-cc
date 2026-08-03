using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game;
using osucc.Core;

namespace osucc.UI.Plugins
{
    /// <summary>
    /// Owns the <see cref="PluginsOverlay"/>, registered with the game's
    /// <see cref="osu.Game.Overlays.IOverlayManager"/> once the game has loaded. Exposes a
    /// static <see cref="Instance"/> so the Specials settings button can open it without
    /// holding its own reference to the game.
    /// </summary>
    public partial class PluginsOverlayComponent : Container
    {
        public static PluginsOverlayComponent? Instance { get; private set; }

        private readonly PluginsOverlay overlay;
        private IDisposable? overlayRegistration;

        [Resolved]
        private OsuGameBase game { get; set; } = null!;

        public PluginsOverlayComponent()
        {
            Instance = this;
            overlay = new PluginsOverlay();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Schedule(registerOverlay);
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

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            overlayRegistration?.Dispose();

            if (ReferenceEquals(Instance, this))
                Instance = null;
        }
    }
}
