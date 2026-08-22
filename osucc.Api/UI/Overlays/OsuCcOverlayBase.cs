using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Input.Events;
using osu.Game.Graphics.Containers;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using System.Collections.Generic;

namespace osucc.UI.Overlays
{
    public abstract partial class OsuCcOverlayBase : OsuFocusedOverlayContainer
    {
        public new const float Padding = 14;

        private static readonly List<OsuCcOverlayBase> registeredOverlays = new();

        protected const double FadeInDuration = 400;
        protected const double FadeOutDuration = 500;

        protected virtual double PopInFadeDuration => FadeInDuration;
        protected virtual double PopOutFadeDuration => FadeOutDuration;

        [Cached]
        public OverlayColourProvider ColourProvider { get; }

        protected Drawable Header { get; private set; } = null!;
        protected PopoverContainer MainAreaContent { get; private set; } = null!;

        protected override bool StartHidden => true;
        protected override bool BlockNonPositionalInput => true;
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

        protected abstract Drawable CreateBackdrop();
        protected abstract float HeaderHeight { get; }
        protected abstract Drawable CreateHeader();

        protected virtual void OnOverlayShown() { }
        protected virtual void OnOverlayHidden() { }

        public virtual void ChangeColourScheme(OverlayColourScheme scheme)
        {
            ColourProvider.ChangeColourScheme(scheme);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            lock (registeredOverlays)
                registeredOverlays.Add(this);
        }

        public static void HideAll()
        {
            lock (registeredOverlays)
            {
                foreach (var overlay in registeredOverlays)
                {
                    if (overlay.State.Value == osu.Framework.Graphics.Containers.Visibility.Visible)
                        overlay.Hide();
                }
            }
        }

        public override void Show()
        {
            if (State.Value == osu.Framework.Graphics.Containers.Visibility.Visible)
            {
                State.TriggerChange();
            }
            else
            {
                if (Client.ClientApi.Game != null)
                {
                    var closeMethod = Core.Reflection.GetMethod("osu.Game.OsuGame", "CloseAllOverlays");
                    closeMethod?.Invoke(Client.ClientApi.Game, new object[] { false });
                }
                base.Show();
            }
        }

        protected override void PopIn()
        {
            lock (registeredOverlays)
            {
                foreach (var overlay in registeredOverlays)
                {
                    if (!ReferenceEquals(overlay, this) && overlay.State.Value == osu.Framework.Graphics.Containers.Visibility.Visible)
                    {
                        overlay.Hide();
                    }
                }
            }

            (Parent as Container)?.ChangeChildDepth(this, (float)-Clock.CurrentTime);

            this.FadeIn(PopInFadeDuration, Easing.OutQuint);

            OnOverlayShown();
        }

        protected override void PopOut()
        {
            base.PopOut();
            (Parent as Container)?.ChangeChildDepth(this, 0);

            this.FadeOut(PopOutFadeDuration, Easing.OutQuint);

            OnOverlayHidden();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            lock (registeredOverlays)
                registeredOverlays.Remove(this);
        }
    }
}
