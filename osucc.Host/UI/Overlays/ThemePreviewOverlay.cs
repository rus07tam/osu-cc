using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osucc.Client;
using osucc.Core;
using osucc.Localisation;
using osucc.Patches;
using osuTK;
using osuTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace osucc.UI.Overlays
{
    /// <summary>
    /// Live preview of the cosmetic UI themes (<see cref="OsuCcThemeDefinition"/>) without a restart.
    /// Shows a representative set of stock UI widgets (buttons, switches, sliders, tabs, cards, ...)
    /// rebuilt from the game's <see cref="OverlayColourProvider"/> so the chrome reflects the active
    /// theme — the runtime patch reads <see cref="OsuCcThemeManager.Active"/> at call time, and the
    /// accent fields are re-painted from their vanilla baseline (see <see cref="OsuColourThemePatch"/>
    /// and <see cref="OsuCcColours"/>), so switching themes previews in place.
    ///
    /// Choosing a theme applies it immediately for preview but does not persist anything. The bottom
    /// 'Apply & restart' button commits it to <see cref="ClientConfig.OsuCcTheme"/> and shows the
    /// restart dialog; 'Cancel' restores the previously saved theme.
    /// </summary>
    public partial class ThemePreviewOverlay : OsuCcShearedOverlay
    {
        private readonly Bindable<OsuCcThemeDefinition> previewTheme = new();
        // The persisted theme, captured at construction. Cancel restores this, and starting the
        // preview on a just-selected theme (from settings) never changes it.
        private readonly OsuCcThemeDefinition savedTheme;

        private Container previewRoot = null!;
        private OverlayScrollContainer scrollContainer = null!;

        public ThemePreviewOverlay()
            : base(OverlayColourScheme.Green)
        {
            savedTheme = OsuCcThemeRegistry.TryGet(ClientConfig.OsuCcTheme.Value, out var persisted)
                ? persisted
                : OsuCcThemeRegistry.Get(OsuCcThemeRegistry.DefaultId);

            previewTheme.Value = savedTheme;
        }

        /// <summary>Starts the preview on a specific theme (e.g. the one just chosen in settings), without changing the restore baseline.</summary>
        public void StartOn(OsuCcThemeDefinition theme) => previewTheme.Value = theme;

        private readonly osu.Framework.Bindables.BindableBool isDirty = new();

        [BackgroundDependencyLoader]
        private void load()
        {
            isDirty.BindTo(osucc.Core.OsuCcThemeManager.IsActiveThemeDirty);
            isDirty.BindValueChanged(change =>
            {
                Header.Title = change.NewValue
                    ? new osu.Framework.Localisation.LocalisableString(ThemePreviewStrings.Title.ToString() + " [DIRTY]")
                    : ThemePreviewStrings.Title;
            }, true);

            Header.Description = ThemePreviewStrings.Description;

            previewRoot = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = new MarginPadding { Bottom = Padding },
            };

            MainAreaContent.Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    scrollContainer = new OverlayScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Top = Padding,
                            Bottom = 46,
                        },
                        Child = previewRoot,
                    },
                    createFooter(),
                },
            });

            previewTheme.BindValueChanged(change =>
            {
                if (change.NewValue == change.OldValue)
                    return;

                applyTheme(change.NewValue);
            }, true);

            // Build the demo widget set once so it is present on first open instead of waiting for a
            // theme change (the initial bindable fire is swallowed by the NewValue == OldValue guard).
            rebuildPreview();
        }

        /// <summary>Persists the previewed theme and closes with the restart dialog.</summary>
        private void confirm()
        {
            ClientConfig.OsuCcTheme.Value = previewTheme.Value.Id;

            var dialogOverlay = ClientApi.Game == null ? null : Reflection.GetDialogOverlay(ClientApi.Game);
            if (dialogOverlay == null)
            {
                ClientNotifications.Error(ThemePreviewStrings.ApplyFailed);
                applyTheme(savedTheme);
                Hide();
                return;
            }

            Hide();
            dialogOverlay.Push(new OsuCcRestartDialog(
                SpecialsSettingsStrings.ThemeRestartTitle,
                SpecialsSettingsStrings.ThemeRestartBody,
                SpecialsSettingsStrings.ThemeRestartButton,
                () => ClientApi.Game?.Exit()));
        }

        /// <summary>Restores the saved theme and closes the preview.</summary>
        private void cancel()
        {
            applyTheme(savedTheme);
            Hide();
        }

        /// <summary>
        /// Activates the given theme for live preview: pins it in <see cref="OsuCcThemeManager"/>,
        /// re-paints the game's accent fields from their vanilla baseline, then rebuilds the preview
        /// widgets so they re-read the now-themed chrome.
        /// </summary>
        private void applyTheme(OsuCcThemeDefinition theme)
        {
            OsuCcThemeManager.ApplyToGame(ClientApi.Game, theme);

            rebuildPreview();
            TimingLog.Info($"ThemePreviewOverlay: previewed {theme.Id}");
        }

        /// <summary>Disposes and recreates the preview widget set to re-read the current theme.</summary>
        private void rebuildPreview()
        {
            var content = new BuildPreview();

            previewRoot.Clear(true);
            previewRoot.Child = content;
        }

        protected override void PopOut()
        {
            base.PopOut();

            // Any close path (header X, click outside, back) that wasn't Cancel/Apply restores the
            // saved theme so a dismissed preview never leaves a temporary theme active.
            if (!isCommitting && OsuCcThemeManager.ActiveId != savedTheme.Id)
                OsuCcThemeManager.ApplyToGame(ClientApi.Game, savedTheme);
        }

        private bool isCommitting;

        private FillFlowContainer createFooter()
        {
            isCommitting = false;

            var themeDropdown = new UpwardThemeDropdown
            {
                Current = previewTheme,
                Width = 220,
            };

            var apply = new RoundedButton
            {
                Text = ThemePreviewStrings.ApplyButton,
                RelativeSizeAxes = Axes.None,
                Width = 140,
                Height = 40,
                Action = () =>
                {
                    isCommitting = true;
                    confirm();
                },
            };

            var cancelButton = new RoundedButton
            {
                Text = ThemePreviewStrings.CancelButton,
                RelativeSizeAxes = Axes.None,
                Width = 140,
                Height = 40,
                Action = () =>
                {
                    isCommitting = true;
                    cancel();
                },
            };

            return new FillFlowContainer
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(12, 0),
                Padding = new MarginPadding { Bottom = Padding },
                Children = new Drawable[]
                {
                    themeDropdown,
                    apply,
                    cancelButton,
                },
            };
        }

        /// <summary>
        /// A <see cref="OsuDropdown{T}"/> over <see cref="OsuCcThemeDefinition"/> whose menu opens
        /// upward instead of downward: the menu is pulled out of the dropdown's internal layout
        /// (which would otherwise grow the dropdown's own height and, because the footer is
        /// bottom-anchored and auto-sized, push the whole row up). The dropdown keeps a fixed
        /// header-sized height and the menu is anchored to its bottom edge, so it expands over the
        /// content above when opened.
        /// </summary>
        private sealed partial class UpwardThemeDropdown : OsuDropdown<OsuCcThemeDefinition>
        {
            public UpwardThemeDropdown()
            {
                OsuCcThemeRegistry.RegisteredThemes.BindCollectionChanged((sender, e) =>
                {
                    Items = OsuCcThemeRegistry.RegisteredThemes;
                }, true);

                AutoSizeAxes = Axes.None;
                Height = Header.Height + Header.Margin.Bottom;

                if (InternalChild is FillFlowContainer<Drawable> flow)
                    flow.Remove(Menu, false);

                Menu.Anchor = Anchor.BottomCentre;
                Menu.Origin = Anchor.BottomCentre;
                Menu.Y = 0;

                AddInternal(Menu);
            }

            protected override LocalisableString GenerateItemText(OsuCcThemeDefinition item) => item.Name;
        }

        /// <summary>
        /// Builds a fresh set of representative stock widgets on the overlay's cached
        /// <see cref="OverlayColourProvider"/>, so colours are read from the currently active theme.
        /// </summary>
        private sealed partial class BuildPreview : Container
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public BuildPreview()
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
            }

            private enum DemoCategory
            {
                General,
                Display,
                Audio,
            }

            private readonly Bindable<DemoCategory> demoCategory = new(DemoCategory.General);

            [BackgroundDependencyLoader]
            private void load()
            {
                var leftColumn = createColumn(new Drawable[]
                {
                    section("Surfaces"),
                    surfaceStrip(),
                    section("Controls"),
                    buttonsRow(),
                    iconRow(),
                    switchesRow(),
                    inputsRow(),
                });

                var rightColumn = createColumn(new Drawable[]
                {
                    section("Selection"),
                    tabsRow(),
                    dropdownRow(),
                    progressRow(),
                    section("Text & accents"),
                    textSample(),
                    accentsRow(),
                });

                Child = new FillFlowContainer
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(28, 0),
                    Padding = new MarginPadding { Horizontal = OsuCcShearedOverlay.Padding * 2 },
                    Children = new Drawable[] { leftColumn, rightColumn },
                };
            }

            private static FillFlowContainer createColumn(IEnumerable<Drawable> children)
            {
                return new FillFlowContainer
                {
                    Width = 330,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 12),
                    Children = children.ToList(),
                };
            }

            private OsuSpriteText section(string text) => new OsuSpriteText
            {
                Text = text,
                Font = OsuFont.Torus.With(size: 16, weight: FontWeight.Bold),
                Colour = colourProvider.Content1,
            };

            private FillFlowContainer surfaceStrip()
            {
                var colours = new[]
                {
                    colourProvider.Background1,
                    colourProvider.Background3,
                    colourProvider.Background5,
                    colourProvider.Background6,
                    colourProvider.Dark2,
                    colourProvider.Dark4,
                    colourProvider.Dark6,
                    colourProvider.Foreground1,
                };

                return new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 6),
                    Children = new Drawable[]
                    {
                        createStripRow(colours),
                        createPanelCard(),
                    },
                };
            }

            private static GridContainer createStripRow(Color4[] colours)
            {
                var cells = new List<Drawable>();

                foreach (var colour in colours)
                {
                    cells.Add(new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 28,
                        Masking = true,
                        CornerRadius = 6,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = colour },
                        },
                    });
                }

                return new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 28,
                    ColumnDimensions = new Dimension[colours.Length].Select(_ => new Dimension()).ToArray(),
                    Content = new[] { cells.ToArray() },
                };
            }

            private Container createPanelCard()
            {
                return new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Masking = true,
                    CornerRadius = 10,
                    Children = new Drawable[]
                    {
                        new Box { RelativeSizeAxes = Axes.Both, Colour = colourProvider.Background4 },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 4),
                            Padding = new MarginPadding(14),
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = "Panel title",
                                    Font = OsuFont.Torus.With(size: 16, weight: FontWeight.Bold),
                                    Colour = colourProvider.Content1,
                                },
                                new OsuSpriteText
                                {
                                    Text = "Body text describing the surface this card renders on.",
                                    Font = OsuFont.Default.With(size: 12),
                                    Colour = colourProvider.Content2,
                                },
                            },
                        },
                    },
                };
            }

            private FillFlowContainer buttonsRow()
            {
                return new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(6, 0),
                    Children = new Drawable[]
                    {
                        new RoundedButton { Text = "Primary", RelativeSizeAxes = Axes.None, Width = 100, Height = 36 },
                        new RoundedButton { Text = "Secondary", RelativeSizeAxes = Axes.None, Width = 104, Height = 36, BackgroundColour = colourProvider.Background5 },
                        new ShearedButton { Text = "Sheared", Width = 96, Height = 36 },
                    },
                };
            }

            private FillFlowContainer iconRow()
            {
                return new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(8, 0),
                    Children = new Drawable[]
                    {
                        new IconButton { Icon = FontAwesome.Solid.Heart, IconColour = colourProvider.Highlight1 },
                        new IconButton { Icon = FontAwesome.Solid.Cog, IconColour = colourProvider.Content1 },
                        new IconButton { Icon = FontAwesome.Solid.Palette, IconColour = colourProvider.Colour2 },
                        new TwoLayerButton
                        {
                            Text = "Layered",
                            Width = 140,
                            Height = 34,
                            BackgroundColour = colourProvider.Background5,
                        },
                    },
                };
            }

            private FillFlowContainer switchesRow()
            {
                return new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(16, 0),
                    Children = new Drawable[]
                    {
                        // OsuCheckbox is RelativeSizeAxes.X internally, so it needs an explicit
                        // width and non-relative sizing to avoid consuming the entire row width.
                        new OsuCheckbox
                        {
                            LabelText = "Checkbox",
                            Current = new BindableBool(true),
                            RelativeSizeAxes = Axes.None,
                            Width = 150,
                            Margin = new MarginPadding { Top = 2 },
                        },
                        labelledSwitch("Switch"),
                    },
                };
            }

            private FillFlowContainer labelledSwitch(string label)
            {
                return new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(6, 0),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText { Text = label, Font = OsuFont.Default.With(size: 13), Colour = colourProvider.Content2 },
                        new SwitchButton { Current = new BindableBool(false) },
                    },
                };
            }

            private static FillFlowContainer inputsRow()
            {
                return new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 8),
                    Children = new Drawable[]
                    {
                        new FormTextBox
                        {
                            RelativeSizeAxes = Axes.X,
                            PlaceholderText = "Text input",
                            Current = new Bindable<string>(),
                        },
                        new FormSliderBar<float>
                        {
                            RelativeSizeAxes = Axes.X,
                            Caption = "Slider",
                            Current = new BindableNumber<float>(0.5f) { MinValue = 0, MaxValue = 1, Precision = 0.01f },
                        },
                    },
                };
            }

            private Container tabsRow()
            {
                return new Container
                {
                    Height = 30,
                    RelativeSizeAxes = Axes.X,
                    Child = new OsuTabControl<DemoCategory>
                    {
                        RelativeSizeAxes = Axes.Both,
                        AccentColour = colourProvider.Highlight1,
                        Current = { BindTarget = demoCategory },
                    },
                };
            }

            private static FillFlowContainer dropdownRow()
            {
                return new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(8, 0),
                    Children = new Drawable[]
                    {
                        new OsuDropdown<OsuCcThemeDefinition>
                        {
                            Width = 140,
                            Items = OsuCcThemeRegistry.RegisteredThemes,
                            Current = new Bindable<OsuCcThemeDefinition>(OsuCcThemeRegistry.Get(OsuCcThemeRegistry.DefaultId)),
                        },
                        new OsuTabControlCheckbox
                        {
                            Text = "Pin",
                            Current = new BindableBool(false),
                            Margin = new MarginPadding { Top = 6 },
                        },
                    },
                };
            }

            private Container progressRow()
            {
                return new Container
                {
                    Height = 8,
                    RelativeSizeAxes = Axes.X,
                    Child = new ProgressBar(false)
                    {
                        RelativeSizeAxes = Axes.Both,
                        EndTime = 1,
                        CurrentTime = 0.65,
                        FillColour = colourProvider.Highlight1,
                        BackgroundColour = colourProvider.Background5,
                    },
                };
            }

            private FillFlowContainer textSample()
            {
                return new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 4),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText { Text = "Heading text", Font = OsuFont.Torus.With(size: 24, weight: FontWeight.Bold), Colour = colourProvider.Content1 },
                        new OsuSpriteText { Text = "Body text in a muted content colour.", Font = OsuFont.Default.With(size: 13), Colour = colourProvider.Content2 },
                        new OsuSpriteText { Text = "Highlighted accent text.", Font = OsuFont.Default.With(size: 13), Colour = colourProvider.Highlight1 },
                    },
                };
            }

            private GridContainer accentsRow()
            {
                var accents = new[]
                {
                    colourProvider.Colour0,
                    colourProvider.Colour1,
                    colourProvider.Colour2,
                    colourProvider.Colour3,
                    colourProvider.Colour4,
                    colourProvider.Highlight1,
                    OsuCcColours.Success,
                    OsuCcColours.Pink,
                };

                var cells = new List<Drawable>();

                foreach (var accent in accents)
                {
                    cells.Add(new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 26,
                        Masking = true,
                        CornerRadius = 6,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = accent },
                        },
                    });
                }

                return new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 26,
                    ColumnDimensions = new Dimension[accents.Length].Select(_ => new Dimension()).ToArray(),
                    Content = new[] { cells.ToArray() },
                };
            }
        }
    }
}
