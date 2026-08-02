using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osucc.Client;
using osucc.UI.Overlays;
using osuTK;
using osuTK.Graphics;
using System;

namespace osuccDebug
{
    /// <summary>
    /// Full-screen debug overlay hosting the osu!cc test panels. Built on
    /// <see cref="OsuCcShearedOverlay"/> (sheared header, colour scheme, scrollable main area).
    /// Closed by clicking outside, pressing back, the header close button or toggling the
    /// toolbar button again.
    /// </summary>
    public partial class osuccDebugOverlay : OsuCcShearedOverlay
    {
        private readonly Action<LocalisableString, ClientNotifications.NotificationKind> notify;

        private readonly FillFlowContainer panels;

        public osuccDebugOverlay(Action<LocalisableString, ClientNotifications.NotificationKind> notify)
            : base(OverlayColourScheme.Purple)
        {
            this.notify = notify;

            panels = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 20),
                Padding = new MarginPadding
                {
                    Horizontal = Padding * 2,
                    Vertical = Padding,
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Header.Title = osuccDebugStrings.OverlayTitle;
            Header.Description = osuccDebugStrings.OverlayDescription;

            MainAreaContent.Add(new OverlayScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = panels,
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            panels.Add(new SectionPanel(osuccDebugStrings.NotificationsPanelTitle)
            {
                PanelContent = new NotificationTestPanel(notify),
            });
            panels.Add(new SectionPanel(osuccDebugStrings.CelebrationsPanelTitle)
            {
                PanelContent = new CelebrationTestPanel(),
            });
        }

        /// <summary>A rounded container used to group one debug panel.</summary>
        private sealed partial class SectionPanel : Container
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            private readonly Box background;
            private readonly FillFlowContainer flow;

            public SectionPanel(LocalisableString title)
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                Masking = true;
                CornerRadius = 8;
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Shadow,
                    Radius = 4,
                    Colour = Color4.Black.Opacity(0.25f),
                };

                InternalChildren = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    flow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding
                        {
                            Horizontal = 20,
                            Vertical = 16,
                        },
                        Spacing = new Vector2(0, 12),
                    },
                };

                flow.Add(new OsuSpriteText
                {
                    Text = title,
                    Font = OsuFont.Torus.With(size: 18, weight: FontWeight.Bold),
                });
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                background.Colour = colourProvider.Background4;
            }

            public Drawable PanelContent
            {
                set => flow.Add(value);
            }
        }
    }
}
