using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osucc.Client;
using osucc.Core;
using osucc.Localisation;
using osucc.Plugin;
using osucc.UI.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osucc.UI.Plugins
{
    /// <summary>A rounded card summarising one plugin, with enable/disable toggle and reorder arrows.</summary>
    internal sealed partial class PluginCard : Container
    {
        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private const float iconZoneWidth = 64;
        private const float iconSize = 40;

        private readonly Box background;
        private readonly Box iconBackground;
        private readonly Box iconDivider;
        private readonly OsuSpriteText statusText;
        private SwitchButton switchButton = null!;
        private IconButton upButton = null!;
        private IconButton downButton = null!;
        private readonly BindableBool enabled = new(true);
        private bool syncingFromData;
        private int moveIndex;
        private int moveCount;
        private SpriteIcon? fallbackIcon;

        public PluginEntry Entry { get; }

        public PluginCard(
            PluginEntry entry,
            Action<PluginCard, int> moveRequested)
        {
            Entry = entry;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Masking = true;
            CornerRadius = 8;

            var header = createHeader(entry, moveRequested);

            statusText = new OsuSpriteText
            {
                Text = PluginCardLayout.StatusText(entry),
                Font = OsuFont.Default.With(size: 13),
                Colour = PluginCardLayout.StatusColour(entry.Status),
            };

            var lines = new List<Drawable> { header, statusText };

            if (!string.IsNullOrEmpty(Entry.Description))
            {
                lines.Add(new OsuSpriteText
                {
                    Text = PluginCardLayout.Description(entry),
                    Font = OsuFont.Default.With(size: 13),
                    Colour = Color4.White.Opacity(0.7f),
                });
            }

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Y,
                    Width = iconZoneWidth,
                    Anchor = Anchor.TopLeft,
                    Children = new Drawable[]
                    {
                        iconBackground = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                        PluginCardLayout.CreateIcon(entry, iconSize, out fallbackIcon),
                        iconDivider = new Box
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = 1,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                        },
                    },
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding
                    {
                        Left = iconZoneWidth + 12,
                        Top = 16,
                        Right = 20,
                        Bottom = 16,
                    },
                    Spacing = new Vector2(0, 6),
                    Children = lines,
                },
            };
        }

        protected override bool OnClick(ClickEvent e)
        {
            PluginsOverlayComponent.Instance?.ShowDetails(Entry.Id);
            return true;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            enabled.Value = Entry.Enabled;

            enabled.BindValueChanged(_ =>
            {
                // A change made while mirroring the data in updateVisualState is an external sync
                // (from the data or another page); do not toggle again or toast about it.
                if (syncingFromData)
                    return;

                PluginManager.SetPluginEnabled(Entry.Id, enabled.Value);

                ClientNotifications.Info(enabled.Value
                    ? PluginsOverlayStrings.PluginEnabled(PluginCardLayout.LocalisedName(Entry))
                    : PluginsOverlayStrings.PluginDisabled(PluginCardLayout.LocalisedName(Entry)));
            });

            Entry.StateChanged += updateVisualState;

            updateVisualState();
        }

        protected override void Dispose(bool isDisposing)
        {
            Entry.StateChanged -= updateVisualState;
            base.Dispose(isDisposing);
        }

        /// <summary>Refreshes the card's opacity, interactivity and status line from the plugin's current data.</summary>
        private void updateVisualState()
        {
            float alpha = Entry.Status switch
            {
                PluginStatus.Active or PluginStatus.Error => 1,
                PluginStatus.PendingDisable => 0.6f,
                PluginStatus.PendingDelete => 0.3f,
                _ => 0.45f,
            };

            this.FadeTo(alpha, 200);
            statusText.Text = PluginCardLayout.StatusText(Entry);
            statusText.Colour = PluginCardLayout.StatusColour(Entry.Status);

            enabled.Disabled = Entry.PendingDelete;

            if (enabled.Value != Entry.Enabled)
            {
                syncingFromData = true;
                enabled.Value = Entry.Enabled;
                syncingFromData = false;
            }

            updateArrows();
        }

        /// <summary>Updates the enabled state of the reorder arrows for this position in the list.</summary>
        public void UpdateMoveAvailability(int index, int count)
        {
            moveIndex = index;
            moveCount = count;

            updateArrows();
        }

        private void updateArrows()
        {
            bool canMove = !Entry.PendingDelete;
            bool canUp = canMove && moveIndex > 0;
            bool canDown = canMove && moveIndex < moveCount - 1;

            upButton.Enabled.Value = canUp;
            downButton.Enabled.Value = canDown;

            upButton.FadeTo(canUp ? 1 : 0.35f, 100);
            downButton.FadeTo(canDown ? 1 : 0.35f, 100);
        }

        private GridContainer createHeader(PluginEntry entry, Action<PluginCard, int> moveRequested)
        {
            var text = new OsuSpriteText
            {
                Text = PluginCardLayout.LocalisedName(entry),
                Font = OsuFont.Torus.With(size: 18, weight: FontWeight.Bold),
            };

            var meta = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(0, 0),
                Children = new Drawable[]
                {
                    setRowCentered(PluginCardLayout.CreateAuthorValue(Entry, fontSize: 12)),
                    setRowCentered(new OsuSpriteText
                    {
                        Text = LocalisableString.Format(" \u2022 v{0}", Entry.Version),
                        Font = OsuFont.Default.With(size: 12),
                        Colour = Color4.White.Opacity(0.55f),
                    }),
                    setRowCentered(new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Margin = new MarginPadding { Left = 8 },
                        Child = PluginCardLayout.CreateTagsValue(entry, fontSize: 11, maxShown: 3),
                    }),
                },
            };

            upButton = PluginCardLayout.CreateActionButton(FontAwesome.Solid.ChevronUp, PluginsOverlayStrings.MoveUp, () => moveRequested(this, -1));
            downButton = PluginCardLayout.CreateActionButton(FontAwesome.Solid.ChevronDown, PluginsOverlayStrings.MoveDown, () => moveRequested(this, 1));

            return new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                },
                ColumnDimensions = new[]
                {
                    new Dimension(),
                    new Dimension(GridSizeMode.AutoSize),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 2),
                            Children = new Drawable[]
                            {
                                text,
                                meta,
                            },
                        },
                        new FillFlowContainer
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(6, 0),
                            Children = new Drawable[]
                            {
                                setRowCentered(switchButton = new SwitchButton
                                {
                                    Current = enabled,
                                }),
                                setRowCentered(upButton),
                                setRowCentered(downButton),
                            },
                        },
                    },
                },
            };
        }

        private static T setRowCentered<T>(T drawable)
            where T : Drawable
        {
            drawable.Anchor = Anchor.CentreLeft;
            drawable.Origin = Anchor.CentreLeft;
            return drawable;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            background.Colour = colourProvider.Background4;
            iconBackground.Colour = colourProvider.Background5;
            iconDivider.Colour = colourProvider.Foreground1.Opacity(0.15f);

            upButton.IconColour = colourProvider.Foreground1;
            downButton.IconColour = colourProvider.Foreground1;

            if (fallbackIcon != null)
                fallbackIcon.Colour = colourProvider.Content1;
        }
    }
}
