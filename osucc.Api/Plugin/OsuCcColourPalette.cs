using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osucc.Localisation;
using osuTK;
using osuTK.Graphics;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace osucc.Plugin
{
    /// <summary>
    /// Colour palette with visible per-swatch delete and reorder controls. The stock
    /// <see cref="FormColourPalette"/> only deletes via a right-click context menu and cannot
    /// reorder at all, so this mirrors its appearance and persistence contract (colours as a
    /// comma-separated hex string) but adds an × remove button and ◀/▶ move buttons.
    /// </summary>
    public partial class OsuCcColourPalette : CompositeDrawable
    {
        public BindableList<Colour4> Colours { get; } = new BindableList<Colour4>();

        public LocalisableString Caption { get; init; }
        public LocalisableString HintText { get; init; }

        private FormControlBackground background = null!;
        private FormFieldCaption caption = null!;
        private FillFlowContainer flow = null!;
        private RoundedButton addButton = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChildren = new Drawable[]
            {
                background = new FormControlBackground(),
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding(9),
                    Spacing = new Vector2(7),
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        caption = new FormFieldCaption
                        {
                            Caption = Caption,
                            TooltipText = HintText,
                        },
                        flow = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Full,
                            Spacing = new Vector2(5),
                            Child = addButton = new RoundedButton
                            {
                                Action = addNewColour,
                                Size = new Vector2(70),
                                Text = "+",
                            }
                        }
                    },
                },
            };

            flow.SetLayoutPosition(addButton, float.MaxValue);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Colours.BindCollectionChanged((_, args) =>
            {
                if (args.Action != NotifyCollectionChangedAction.Replace)
                    updateColours();
            }, true);
            updateState();
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateState();
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);
            updateState();
        }

        private void addNewColour()
        {
            Color4 startingColour = Colours.Count > 0
                ? Colours.Last()
                : Colour4.White;

            Colours.Add(startingColour);
            flow.OfType<PaletteItem>().Last().TriggerPick();
        }

        private void updateState()
        {
            caption.Colour = colourProvider.Content2;

            if (IsHovered)
                background.VisualStyle = VisualStyle.Hovered;
            else
                background.VisualStyle = VisualStyle.Normal;
        }

        private void updateColours()
        {
            flow.RemoveAll(d => d is PaletteItem, true);

            for (int i = 0; i < Colours.Count; ++i)
            {
                // copy to avoid accesses to modified closure.
                int colourIndex = i;

                var item = new PaletteItem
                {
                    CanMoveLeft = colourIndex > 0,
                    CanMoveRight = colourIndex < Colours.Count - 1,
                    Current = { Value = Colours[colourIndex] }
                };

                item.Current.BindValueChanged(colour => Colours[colourIndex] = colour.NewValue);
                item.MoveRequested = delta => Colours.Move(colourIndex, colourIndex + delta);
                item.DeleteRequested = () => Colours.RemoveAt(colourIndex);
                flow.Add(item);
            }
        }

        /// <summary>One palette entry: a pickable swatch above its reorder/delete buttons.</summary>
        private sealed partial class PaletteItem : CompositeDrawable
        {
            public Bindable<Colour4> Current { get; } = new Bindable<Colour4>();

            public Action<int>? MoveRequested { get; set; }
            public Action? DeleteRequested { get; set; }

            public bool CanMoveLeft { get; init; }
            public bool CanMoveRight { get; init; }

            private Swatch swatch = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                AutoSizeAxes = Axes.Both;

                InternalChild = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(3),
                    Children = new Drawable[]
                    {
                        swatch = new Swatch
                        {
                            Current = { BindTarget = Current },
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(2),
                            Children = new Drawable[]
                            {
                                new PaletteButton
                                {
                                    Icon = FontAwesome.Solid.ArrowLeft,
                                    TooltipText = OsuCcStrings.MoveLeft,
                                    Action = () => MoveRequested?.Invoke(-1),
                                    Enabled = { Value = CanMoveLeft },
                                },
                                new PaletteButton
                                {
                                    Icon = FontAwesome.Solid.Times,
                                    TooltipText = OsuCcStrings.Delete,
                                    Destructive = true,
                                    Action = () => DeleteRequested?.Invoke(),
                                },
                                new PaletteButton
                                {
                                    Icon = FontAwesome.Solid.ArrowRight,
                                    TooltipText = OsuCcStrings.MoveRight,
                                    Action = () => MoveRequested?.Invoke(1),
                                    Enabled = { Value = CanMoveRight },
                                },
                            },
                        },
                    },
                };
            }

            public void TriggerPick() => swatch.ShowPopover();
        }

        /// <summary>Clickable colour swatch opening an <see cref="OsuColourPicker"/> popover.</summary>
        private sealed partial class Swatch : OsuClickableContainer, IHasPopover
        {
            public Bindable<Colour4> Current { get; } = new Bindable<Colour4>();

            private Box background = null!;
            private OsuSpriteText hexCode = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                Size = new Vector2(70);

                Masking = true;
                CornerRadius = 10;
                CornerExponent = 2.5f;
                Action = this.ShowPopover;

                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    hexCode = new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Current.BindValueChanged(_ => updateState(), true);
            }

            public Popover GetPopover() => new ColourPickerPopover
            {
                Current = { BindTarget = Current },
            };

            private void updateState()
            {
                background.Colour = Current.Value;
                hexCode.Text = Current.Value.ToHex();
                hexCode.Colour = OsuColour.ForegroundTextColourFor(Current.Value);
            }
        }

        /// <summary>Small icon button with hover feedback, used for the reorder/delete controls.</summary>
        private sealed partial class PaletteButton : OsuClickableContainer
        {
            public IconUsage Icon { get; init; }
            public new LocalisableString TooltipText { get; set; }

            /// <summary>Tints the icon red (and brighter on hover) instead of the palette highlight.</summary>
            public bool Destructive { get; init; }

            public Color4 IconColour { get; init; } = Color4.White;

            private SpriteIcon icon = null!;
            private Box background = null!;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                Size = new Vector2(20);

                Masking = true;
                CornerRadius = 5;
                CornerExponent = 2.5f;

                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                    },
                    icon = new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(10),
                        Icon = Icon,
                        Colour = IconColour,
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Enabled.BindValueChanged(_ => updateState());
                updateState();
            }

            protected override bool OnHover(HoverEvent e)
            {
                updateState();
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                base.OnHoverLost(e);
                updateState();
            }

            private void updateState()
            {
                bool enabled = Enabled.Value;
                Color4 restColour = Destructive ? colours.Red : IconColour;

                this.FadeTo(enabled ? 1 : 0.3f, 200);
                background.FadeTo(enabled && IsHovered ? 1 : 0, 200);
                icon.FadeColour(enabled && IsHovered ? (Destructive ? colours.Red.Lighten(0.2f) : colourProvider.Highlight1) : restColour, 200);
            }
        }

        private sealed partial class ColourPickerPopover : OsuPopover, IHasCurrentValue<Colour4>
        {
            public Bindable<Colour4> Current
            {
                get => current.Current;
                set => current.Current = value;
            }

            private readonly BindableWithCurrent<Colour4> current = new BindableWithCurrent<Colour4>();

            public ColourPickerPopover()
                : base(false)
            {
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider)
            {
                Child = new OsuColourPicker
                {
                    Current = { BindTarget = Current },
                };

                Body.BorderThickness = 2;
                Body.BorderColour = colourProvider.Highlight1;
                Content.Padding = new MarginPadding(2);
            }
        }
    }
}
