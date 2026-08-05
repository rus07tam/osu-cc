using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;

namespace osucc.UI.Overlays
{
    /// <summary>
    /// Base class for osu!cc's sheared full-screen overlays: dimmed main content, canonical
    /// background, sheared header with title/description/close, scrollable main area. Not based on
    /// <see cref="osu.Game.Overlays.Mods.ShearedOverlayContainer"/>: that registers itself as the
    /// footer's active overlay in <c>PopIn()</c>, and <see cref="osu.Game.Screens.Footer.ScreenFooter"/>
    /// only allows a single active footer overlay, so opening two such overlays in a row throws
    /// "Cannot set overlay content while one is already present". Deriving from
    /// <see cref="OsuFocusedOverlayContainer"/> (like the game's toolbar overlays) keeps us in the
    /// plain <c>overlayContent</c> layer, so multiple osu!cc overlays open independently.
    /// </summary>
    public abstract partial class OsuCcShearedOverlay : OsuFocusedOverlayContainer
    {
        public new const float Padding = 14;

        /// <summary>
        /// Every live osu!cc overlay. Showing one hides the others, so the most recently opened
        /// overlay always renders on top instead of stacking behind previously opened ones.
        /// </summary>
        private static readonly List<OsuCcShearedOverlay> registeredOverlays = new();

        /// <summary>
        /// The overlay that was visible when this one opened (hidden by the mutual exclusion).
        /// Closed explicitly (header close, click outside, back) this overlay returns to it.
        /// </summary>
        private OsuCcShearedOverlay? previousOverlay;

        private bool restorePrevious;

        private const double fadeInDuration = 400;
        private const double fadeOutDuration = 500;

        // Depth raised while shown so this overlay renders above the game's own (depth-0) overlays
        // in the shared overlayContent layer. Negative so it is always in front of them.
        private const float showDepth = -1;

        [Cached]
        public OverlayColourProvider ColourProvider { get; }

        /// <summary>The sheared header (title/description/close).</summary>
        protected ShearedOverlayHeader Header { get; private set; } = null!;

        /// <summary>
        /// Content displayed below the header. A <see cref="PopoverContainer"/> so popovers
        /// (e.g. the colour picker in <see cref="osucc.Plugin.OsuCcColourPalette"/>) find a parent
        /// container: the game's only PopoverContainer lives in ScreenContainer, which is not an
        /// ancestor of this overlay layer.
        /// </summary>
        protected PopoverContainer MainAreaContent { get; private set; } = null!;

        protected override bool StartHidden => true;

        protected override bool BlockNonPositionalInput => true;

        // Use the game's shared blocking-overlay dim (via IOverlayManager.ShowBlockingOverlay).
        // The dim targets ScreenContainer, which does not contain the overlayContent layer this
        // overlay lives on, so we never dim ourselves; multiple visible overlays share one dim.
        protected override bool DimMainContent => true;

        protected OsuCcShearedOverlay(OverlayColourScheme colourScheme)
        {
            RelativeSizeAxes = Axes.Both;

            ColourProvider = new OverlayColourProvider(colourScheme);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    // Canonical sheared-overlay background (same as the game's ShearedOverlayContainer).
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourProvider.Background6.Opacity(0.75f),
                    },
                    Header = new ShearedOverlayHeader
                    {
                        Anchor = Anchor.TopCentre,
                        Depth = float.MinValue,
                        Origin = Anchor.TopCentre,
                        Close = closeWithRestore,
                    },
                    MainAreaContent = new PopoverContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Top = ShearedOverlayHeader.HEIGHT,
                            Bottom = Padding,
                        },
                    },
                }
            };
        }

        /// <summary>Hides this overlay, restoring the overlay it was opened on top of (if any).</summary>
        private void closeWithRestore()
        {
            restorePrevious = true;
            Hide();
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (State.Value == Visibility.Visible)
            {
                closeWithRestore();
                return true;
            }

            return base.OnClick(e);
        }

        public override bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Repeat)
                return false;

            if (e.Action == GlobalAction.Back)
            {
                closeWithRestore();
                return true;
            }

            return base.OnPressed(e);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            lock (registeredOverlays)
                registeredOverlays.Add(this);
        }

        protected override void PopIn()
        {
            // "Last opened wins": hide every other visible osu!cc overlay so the newly opened one
            // is never rendered behind previously opened overlays in the shared overlayContent layer.
            lock (registeredOverlays)
            {
                foreach (var overlay in registeredOverlays)
                {
                    if (!ReferenceEquals(overlay, this) && overlay.State.Value == Visibility.Visible)
                    {
                        previousOverlay = overlay;
                        overlay.Hide();
                    }
                }
            }

            // The game places all full-screen overlays in the single overlayContent container; ours
            // are registered early, so without this they would sit at the bottom of that layer and
            // render behind any other visible overlay. Raising the depth keeps the freshly opened
            // overlay on top (lower depth = drawn in front).
            (Parent as Container)?.ChangeChildDepth(this, showDepth);

            this.FadeIn(fadeInDuration, Easing.OutQuint);
            Header.MoveToY(0, fadeInDuration, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            base.PopOut();
            (Parent as Container)?.ChangeChildDepth(this, 0);

            this.FadeOut(fadeOutDuration, Easing.OutQuint);
            Header.MoveToY(-Header.DrawHeight, fadeOutDuration, Easing.OutQuint);

            // Only explicit user closes restore the previous overlay. Bulk hides (CloseAllOverlays,
            // plugin toolbar toggles, opening another overlay) leave it hidden.
            if (!restorePrevious)
                return;

            restorePrevious = false;

            var toRestore = previousOverlay;
            previousOverlay = null;
            toRestore?.Show();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            lock (registeredOverlays)
                registeredOverlays.Remove(this);
        }
    }
}
