using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game;
using osucc.Core;
using osucc.UI.Overlays;

namespace osucc.UI.Overlays
{
    /// <summary>
    /// Owns the <see cref="ThemePreviewOverlay"/>, registered with the game's
    /// <see cref="osu.Game.Overlays.IOverlayManager"/> once the game has loaded. Exposes a static
    /// <see cref="Instance"/> so the Specials settings section can open the theme preview without
    /// holding its own reference to the game.
    /// </summary>
    public partial class ThemePreviewComponent : Container
    {
        public static ThemePreviewComponent? Instance { get; private set; }

        private readonly ThemePreviewOverlay overlay;
        private IDisposable? overlayRegistration;

        [Resolved]
        private OsuGameBase game { get; set; } = null!;

        public ThemePreviewComponent()
        {
            Instance = this;
            overlay = new ThemePreviewOverlay();
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

            TimingLog.Info("Theme preview overlay registered via IOverlayManager");
        }

        /// <summary>Shows the theme preview overlay. If the user is changing the theme, start it on that one.</summary>
        public void Show(OsuCcThemeDefinition? startOn = null)
        {
            if (startOn != null)
                overlay.StartOn(startOn);

            overlay.Show();
            TimingLog.Info("Theme preview overlay shown");
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
