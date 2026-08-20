using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Input.Events;
using osu.Game.Graphics.Containers;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;

namespace osucc.UI.Overlays
{
    /// <summary>
    /// Base class for osu!cc's full-screen overlays: dimmed main content, canonical background,
    /// a header with title/description/close and a scrollable main area. Not based on
    /// <see cref="osu.Game.Overlays.Mods.ShearedOverlayContainer"/>: that registers itself as the
    /// footer's active overlay in <c>PopIn()</c>, and <see cref="osu.Game.Screens.Footer.ScreenFooter"/>
    /// only allows a single active footer overlay, so opening two such overlays in a row throws
    /// "Cannot set overlay content while one is already present". Deriving from
    /// <see cref="OsuFocusedOverlayContainer"/> (like the game's toolbar overlays) keeps us in the
    /// plain <c>overlayContent</c> layer, so multiple osu!cc overlays open independently.
    /// <para>
    /// Concrete styles subclass this and provide a backdrop and a header (<see cref="OsuCcShearedOverlay"/>
    /// and <see cref="OsuCcWaveOverlay"/>).
    /// </para>
    /// </summary>
    public abstract partial class OsuCcOverlayBase : OsuFocusedOverlayContainer
    {
        public new const float Padding = 14;

        private static readonly List<OsuCcOverlayBase> registeredOverlays = new();

        private OsuCcOverlayBase? previousOverlay;

        private bool restorePrevious;

        protected const double FadeInDuration = 400;
        protected const double FadeOutDuration = 500;

        /// <summary>Duration of the whole-overlay fade on open. Styles override for their own feel
        /// (the wave style reveals fast, the sheared style fades in slowly).</summary>
        protected virtual double PopInFadeDuration => FadeInDuration;

        /// <summary>Duration of the whole-overlay fade on close.</summary>
        protected virtual double PopOutFadeDuration => FadeOutDuration;

        private const float showDepth = -1;

        [Cached]
        public OverlayColourProvider ColourProvider { get; }

        /// <summary>The header created by <see cref="CreateHeader"/> (title/description/close).</summary>
        protected Drawable Header { get; private set; } = null!;

        /// <summary>
        /// Content displayed below the header. A <see cref="PopoverContainer"/> so popovers
        /// (e.g. the colour picker in <see cref="Plugin.OsuCcColourPalette"/>) find a parent
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

        protected OsuCcOverlayBase(OverlayColourScheme colourScheme)
        {
            RelativeSizeAxes = Axes.Both;

            ColourProvider = new OverlayColourProvider(colourScheme);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Header = CreateHeader();
            MainAreaContent = new PopoverContainer
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding
                {
                    Top = HeaderHeight,
                    Bottom = Padding,
                },
            };

            Child = ComposeContent(CreateBackdrop());
        }

        /// <summary>
        /// Assembles the overlay's child tree from the background and the header/main content.
        /// The default layout places the header over the backdrop with the main content below it;
        /// styles may override to arrange them differently (e.g. the wave style puts both inside a
        /// shared scroll container).
        /// </summary>
        protected virtual Drawable ComposeContent(Drawable backdrop)
        {
            backdrop.Depth = float.MaxValue;

            Header.Anchor = Anchor.TopCentre;
            Header.Origin = Anchor.TopCentre;
            Header.Depth = float.MinValue;

            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    backdrop,
                    Header,
                    MainAreaContent,
                }
            };
        }

        /// <summary>The drawable behind everything (background box, animated backdrop, ...).</summary>
        protected abstract Drawable CreateBackdrop();

        /// <summary>Horizontal space consumed at the top by the header, reserved as top padding for <see cref="MainAreaContent"/>.</summary>
        protected abstract float HeaderHeight { get; }

        /// <summary>The header drawable (with a <c>Close</c> action). Shown on top of the backdrop.</summary>
        protected abstract Drawable CreateHeader();

        /// <summary>Called from <see cref="PopIn"/> once the overlay starts fading in.</summary>
        protected virtual void OnOverlayShown()
        {
        }

        /// <summary>Called from <see cref="PopOut"/> once the overlay starts fading out.</summary>
        protected virtual void OnOverlayHidden()
        {
        }

        /// <summary>Changes the colour scheme of the overlay.</summary>
        public virtual void ChangeColourScheme(OverlayColourScheme scheme)
        {
            ColourProvider.ChangeColourScheme(scheme);
        }

        /// <summary>Hides this overlay, restoring the overlay it was opened on top of (if any).</summary>
        protected void CloseWithRestore()
        {
            restorePrevious = true;
            Hide();
        }

        public override bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Repeat)
                return false;

            if (e.Action == GlobalAction.Back)
            {
                CloseWithRestore();
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

            (Parent as Container)?.ChangeChildDepth(this, showDepth);

            this.FadeIn(PopInFadeDuration, Easing.OutQuint);

            OnOverlayShown();
        }

        protected override void PopOut()
        {
            base.PopOut();
            (Parent as Container)?.ChangeChildDepth(this, 0);

            this.FadeOut(PopOutFadeDuration, Easing.OutQuint);

            OnOverlayHidden();

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
