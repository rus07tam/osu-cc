using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osucc.Core;
using osucc.Localisation;
using osucc.Plugin;
using osuTK;
using osuTK.Graphics;

namespace osucc.UI.Plugins
{
    /// <summary>
    /// A sleek, beatmap-card styled card representing one plugin with interactive hover state,
    /// dedicated icon tile, status indicator, metadata, enable/disable switch, and reorder buttons.
    /// </summary>
    public sealed partial class PluginCard : Container
    {
        public const float CardHeight = 88;
        private const float icon_tile_size = CardHeight;
        private const float icon_size = 44;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly Box background;
        private readonly Box hoverOverlay;
        private readonly Box iconBackground;
        private readonly Box iconDivider;
        private readonly Box versionBadgeBackground;
        private readonly Box statusIndicatorDot;
        private readonly OsuSpriteText statusText;
        private readonly Drawable iconDrawable;
        private readonly SpriteIcon? fallbackIcon;

        private SwitchButton switchButton = null!;
        private IconButton upButton = null!;
        private IconButton downButton = null!;
        private readonly BindableBool enabled = new(true);
        private bool syncingFromData;
        private int moveIndex;
        private int moveCount;

        public PluginEntry Entry { get; }

        public Action<PluginCard, bool>? EnabledChanged { get; set; }
        public Action<PluginCard>? Clicked { get; set; }

        public PluginCard(PluginEntry entry, Action<PluginCard, int>? moveRequested = null)
        {
            Entry = entry;

            RelativeSizeAxes = Axes.X;
            Height = CardHeight;
            Masking = true;
            CornerRadius = 10;

            EdgeEffect = new EdgeEffectParameters
            {
                Type = EdgeEffectType.Shadow,
                Colour = Color4.Black.Opacity(0.18f),
                Radius = 4,
                Offset = new Vector2(0, 2),
            };

            iconDrawable = PluginCardLayout.CreateIcon(entry, icon_size, out fallbackIcon);

            upButton = PluginCardLayout.CreateActionButton(FontAwesome.Solid.ChevronUp, PluginsOverlayStrings.MoveUp, () => moveRequested?.Invoke(this, -1));
            upButton.Anchor = Anchor.CentreLeft;
            upButton.Origin = Anchor.CentreLeft;

            downButton = PluginCardLayout.CreateActionButton(FontAwesome.Solid.ChevronDown, PluginsOverlayStrings.MoveDown, () => moveRequested?.Invoke(this, 1));
            downButton.Anchor = Anchor.CentreLeft;
            downButton.Origin = Anchor.CentreLeft;

            statusText = new OsuSpriteText
            {
                Text = PluginCardLayout.StatusText(entry),
                Font = OsuFont.Default.With(size: 12, weight: FontWeight.Medium),
                Colour = PluginCardLayout.StatusColour(entry.Status),
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
            };

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                },
                hoverOverlay = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White,
                    Alpha = 0,
                },
                // Left Icon Tile
                new Container
                {
                    Size = new Vector2(icon_tile_size),
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Children = new Drawable[]
                    {
                        iconBackground = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                        iconDrawable,
                        // Status dot at top-left
                        new Container
                        {
                            Size = new Vector2(10),
                            Margin = new MarginPadding(8),
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft,
                            Masking = true,
                            CornerRadius = 5,
                            Child = statusIndicatorDot = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = PluginCardLayout.StatusColour(entry.Status),
                            },
                        },
                        iconDivider = new Box
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = 1,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                        },
                    },
                },
                // Center Content Area
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding
                    {
                        Left = icon_tile_size + 14,
                        Right = 240,
                        Vertical = 12,
                    },
                    Spacing = new Vector2(0, 4),
                    Children = new Drawable[]
                    {
                        // Title row: Name + Version Badge
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(8, 0),
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = PluginCardLayout.LocalisedName(entry),
                                    Font = OsuFont.Torus.With(size: 17, weight: FontWeight.Bold),
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                },
                                new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Masking = true,
                                    CornerRadius = 4,
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Children = new Drawable[]
                                    {
                                        versionBadgeBackground = new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                        },
                                        new OsuSpriteText
                                        {
                                            Text = $"v{entry.Version}",
                                            Font = OsuFont.Torus.With(size: 10, weight: FontWeight.Bold),
                                            Colour = Color4.White.Opacity(0.9f),
                                            Padding = new MarginPadding { Horizontal = 6, Vertical = 2 },
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                        },
                                    },
                                },
                            },
                        },
                        // Author + Tags row
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(10, 0),
                            Children = new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(4, 0),
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Children = new Drawable[]
                                    {
                                        new OsuSpriteText
                                        {
                                            Text = "by",
                                            Font = OsuFont.Default.With(size: 12),
                                            Colour = Color4.White.Opacity(0.5f),
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                        },
                                        PluginCardLayout.CreateAuthorValue(entry, fontSize: 12),
                                    },
                                },
                                new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Child = PluginCardLayout.CreateTagsValue(entry, fontSize: 10, maxShown: 3),
                                },
                            },
                        },
                        // Description row
                        new TruncatingSpriteText
                        {
                            Text = PluginCardLayout.Description(entry),
                            Font = OsuFont.Default.With(size: 12),
                            Colour = Color4.White.Opacity(0.65f),
                            RelativeSizeAxes = Axes.X,
                        },
                    },
                },
                // Right Action Controls
                new FillFlowContainer
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(10, 0),
                    Margin = new MarginPadding { Right = 16 },
                    Children = new Drawable[]
                    {
                        statusText,
                        switchButton = new SwitchButton
                        {
                            Current = enabled,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                        upButton,
                        downButton,
                        new SpriteIcon
                        {
                            Icon = FontAwesome.Solid.ChevronRight,
                            Size = new Vector2(12),
                            Colour = Color4.White.Opacity(0.35f),
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Margin = new MarginPadding { Left = 4 },
                        },
                    },
                },
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            hoverOverlay.FadeTo(0.05f, 150, Easing.OutQuint);
            iconDrawable.ScaleTo(1.05f, 200, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            hoverOverlay.FadeTo(0, 150, Easing.OutQuint);
            iconDrawable.ScaleTo(1f, 200, Easing.OutQuint);
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (Clicked != null)
                Clicked(this);
            else
                PluginNameLink.ShowDetailsHandler?.Invoke(Entry.Id);

            return true;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            enabled.Value = Entry.Enabled;

            enabled.BindValueChanged(_ =>
            {
                if (syncingFromData)
                    return;

                EnabledChanged?.Invoke(this, enabled.Value);
            });

            Entry.StateChanged += updateVisualState;
            updateVisualState();
        }

        protected override void Dispose(bool isDisposing)
        {
            Entry.StateChanged -= updateVisualState;
            base.Dispose(isDisposing);
        }

        private void updateVisualState()
        {
            float alpha = Entry.Status switch
            {
                PluginStatus.Active or PluginStatus.Error => 1,
                PluginStatus.PendingDisable => 0.6f,
                PluginStatus.PendingDelete => 0.3f,
                _ => 0.5f,
            };

            this.FadeTo(alpha, 200);
            statusText.Text = PluginCardLayout.StatusText(Entry);
            statusText.Colour = PluginCardLayout.StatusColour(Entry.Status);
            statusIndicatorDot.Colour = PluginCardLayout.StatusColour(Entry.Status);

            enabled.Disabled = Entry.PendingDelete;

            if (enabled.Value != Entry.Enabled)
            {
                syncingFromData = true;
                enabled.Value = Entry.Enabled;
                syncingFromData = false;
            }

            updateArrows();
        }

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

        [BackgroundDependencyLoader]
        private void load()
        {
            background.Colour = colourProvider.Background4;
            iconBackground.Colour = colourProvider.Background5;
            iconDivider.Colour = colourProvider.Foreground1.Opacity(0.08f);
            versionBadgeBackground.Colour = colourProvider.Background3;

            upButton.IconColour = colourProvider.Foreground1;
            downButton.IconColour = colourProvider.Foreground1;

            if (fallbackIcon != null)
                fallbackIcon.Colour = colourProvider.Content1;
        }
    }
}
