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
    /// <summary>
    /// Full-screen overlay listing every discovered plugin (icon, name, author, version,
    /// description, load status). Opened from the Specials settings section.
    /// </summary>
    public partial class PluginsOverlay : OsuCcShearedOverlay
    {
        private readonly FillFlowContainer list;
        private readonly List<PluginCard> cards = new();
        private OverlayScrollContainer scrollContainer = null!;

        public PluginsOverlay()
            : base(OverlayColourScheme.Green)
        {
            list = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 12),
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
            Header.Title = PluginsOverlayStrings.OverlayTitle;
            Header.Description = PluginsOverlayStrings.OverlayDescription;

            MainAreaContent.Add(scrollContainer = new OverlayScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = list,
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var entries = PluginManager.Plugins;

            if (entries.Count == 0)
            {
                list.Add(new OsuSpriteText
                {
                    Text = PluginsOverlayStrings.EmptyState,
                    Font = OsuFont.Default.With(size: 14),
                    Colour = Color4.White.Opacity(0.6f),
                });
                return;
            }

            foreach (var entry in entries)
                cards.Add(new PluginCard(entry, moveCard, deletePlugin));

            foreach (var card in cards)
                list.Add(card);

            applyOrder();
        }

        /// <summary>Refreshes the list layout positions and the up/down availability of every card.</summary>
        private void applyOrder()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                list.SetLayoutPosition(cards[i], i);
                cards[i].UpdateMoveAvailability(i, cards.Count);
            }
        }

        /// <summary>Swaps a card with its neighbour and persists the new order.</summary>
        private void moveCard(PluginCard card, int delta)
        {
            int index = cards.IndexOf(card);
            int target = index + delta;

            if (index < 0 || target < 0 || target >= cards.Count)
                return;

            (cards[index], cards[target]) = (cards[target], cards[index]);

            applyOrder();

            PluginManager.SetPluginOrder(cards.Select(c => c.Entry.Id).ToList());
            ClientNotifications.Info(PluginsOverlayStrings.OrderChanged);
        }

        /// <summary>Asks for confirmation, then marks the plugin for deletion on the next launch.</summary>
        private void deletePlugin(PluginCard card)
        {
            var dialogOverlay = ClientApi.Game == null ? null : Reflection.GetDialogOverlay(ClientApi.Game);

            if (dialogOverlay == null)
            {
                ClientNotifications.Error(PluginsOverlayStrings.ConfirmDialogFailed);
                return;
            }

            LocalisableString name = localisedName(card.Entry);

            dialogOverlay.Push(new OsuCcConfirmDialog(
                PluginsOverlayStrings.DeleteTitle,
                PluginsOverlayStrings.DeleteBody(name),
                () =>
                {
                    PluginManager.RemovePlugin(card.Entry.Id);
                    card.SetPendingDelete();
                    applyOrder();
                    ClientNotifications.Info(PluginsOverlayStrings.DeleteConfirmed(name));
                }));
        }

        /// <summary>Localised plugin name from the <c>&lt;id&gt;:name</c> key, falling back to the attribute value.</summary>
        private static LocalisableString localisedName(PluginEntry entry) => OsuCcLocalisation.Get($"{entry.Id}:name", entry.Name);

        /// <summary>A rounded card summarising one plugin, with enable/disable toggle, reorder arrows and delete.</summary>
        private sealed partial class PluginCard : Container
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
            private IconButton deleteButton = null!;
            private readonly BindableBool enabled = new(true);
            private SpriteIcon? fallbackIcon;

            public PluginEntry Entry { get; }

            public PluginCard(
                PluginEntry entry,
                Action<PluginCard, int> moveRequested,
                Action<PluginCard> deleteRequested)
            {
                Entry = entry;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                Masking = true;
                CornerRadius = 8;

                var header = createHeader(entry, moveRequested, deleteRequested);

                statusText = new OsuSpriteText
                {
                    Text = getStatusText(entry),
                    Font = OsuFont.Default.With(size: 13),
                    Colour = getStatusColour(entry.Status),
                };

                var lines = new List<Drawable> { header, statusText };

                if (!string.IsNullOrEmpty(Entry.Description))
                {
                    lines.Add(new OsuSpriteText
                    {
                        Text = OsuCcLocalisation.Get($"{entry.Id}:description", Entry.Description ?? string.Empty),
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
                    PluginManager.SetPluginEnabled(Entry.Id, enabled.Value);
                    Entry.Enabled = enabled.Value;

                    updateVisualState();
                    ClientNotifications.Info(enabled.Value
                        ? PluginsOverlayStrings.PluginEnabled(localisedName(Entry))
                        : PluginsOverlayStrings.PluginDisabled(localisedName(Entry)));
                });

                updateVisualState();
            }

            /// <summary>Refreshes the card's opacity and status line from the current <see cref="PluginStatus"/>.</summary>
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
                statusText.Text = getStatusText(Entry);
                statusText.Colour = getStatusColour(Entry.Status);
            }

            /// <summary>Updates the enabled state of the reorder arrows for this position in the list.</summary>
            public void UpdateMoveAvailability(int index, int count)
            {
                bool canMove = !Entry.PendingDelete;
                bool canUp = canMove && index > 0;
                bool canDown = canMove && index < count - 1;

                upButton.Enabled.Value = canUp;
                downButton.Enabled.Value = canDown;

                upButton.FadeTo(canUp ? 1 : 0.35f, 100);
                downButton.FadeTo(canDown ? 1 : 0.35f, 100);
            }

            /// <summary>Renders the card as a pending-delete item: dimmed, fully non-interactive, delete button hidden.</summary>
            public void SetPendingDelete()
            {
                Entry.PendingDelete = true;

                enabled.Disabled = true;
                upButton.Enabled.Value = false;
                downButton.Enabled.Value = false;
                deleteButton.Enabled.Value = false;

                deleteButton.FadeOut(200);
                updateVisualState();
            }

            private GridContainer createHeader(PluginEntry entry, Action<PluginCard, int> moveRequested, Action<PluginCard> deleteRequested)
            {
                var text = new OsuSpriteText
                {
                    Text = localisedName(entry),
                    Font = OsuFont.Torus.With(size: 18, weight: FontWeight.Bold),
                };

                var meta = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(0, 0),
                    Children = new Drawable[]
                    {
                        PluginCardLayout.CreateAuthorValue(Entry, fontSize: 12),
                        new OsuSpriteText
                        {
                            Text = LocalisableString.Format(" \u2022 v{0}", Entry.Version),
                            Font = OsuFont.Default.With(size: 12),
                            Colour = Color4.White.Opacity(0.55f),
                        },
                    },
                };

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
                                    switchButton = new SwitchButton
                                    {
                                        Current = enabled,
                                    },
                                    upButton = createActionButton(FontAwesome.Solid.ChevronUp, PluginsOverlayStrings.MoveUp, () => moveRequested(this, -1)),
                                    downButton = createActionButton(FontAwesome.Solid.ChevronDown, PluginsOverlayStrings.MoveDown, () => moveRequested(this, 1)),
                                    deleteButton = createActionButton(FontAwesome.Solid.Trash, PluginsOverlayStrings.DeletePluginTooltip, () => deleteRequested(this)),
                                },
                            },
                        },
                    },
                };
            }

            private static IconButton createActionButton(IconUsage icon, LocalisableString tooltip, Action action) => new()
            {
                Icon = icon,
                TooltipText = tooltip,
                Action = action,
                IconColour = Color4.White,
            };

            private static LocalisableString getStatusText(PluginEntry entry)
            {
                return entry.Status switch
                {
                    PluginStatus.Active => PluginsOverlayStrings.StatusActive,
                    PluginStatus.PendingEnable => PluginsOverlayStrings.StatusPendingEnable,
                    PluginStatus.PendingDisable => PluginsOverlayStrings.StatusPendingDisable,
                    PluginStatus.PendingDelete => PluginsOverlayStrings.StatusPendingDelete,
                    PluginStatus.Disabled => PluginsOverlayStrings.StatusDisabled,
                    PluginStatus.Error => entry.LoadError == null ? PluginsOverlayStrings.StatusFailed : PluginsOverlayStrings.StatusFailedWithError(entry.LoadError.Message),
                    _ => string.Empty,
                };
            }

            private static Color4 getStatusColour(PluginStatus status)
            {
                return status switch
                {
                    PluginStatus.Active => OsuCcColours.Success,
                    PluginStatus.PendingEnable or PluginStatus.PendingDisable => OsuCcColours.Info,
                    PluginStatus.PendingDelete or PluginStatus.Disabled => OsuCcColours.Disabled,
                    PluginStatus.Error => OsuCcColours.Error,
                    _ => OsuCcColours.Info,
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                background.Colour = colourProvider.Background4;
                iconBackground.Colour = colourProvider.Background5;
                iconDivider.Colour = colourProvider.Foreground1.Opacity(0.15f);

                upButton.IconColour = colourProvider.Foreground1;
                downButton.IconColour = colourProvider.Foreground1;
                deleteButton.IconColour = OsuCcColours.Error;

                if (fallbackIcon != null)
                    fallbackIcon.Colour = colourProvider.Content1;
            }
        }
    }
}
