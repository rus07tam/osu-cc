using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.Containers.Markdown;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osucc.Client;
using osucc.Core;
using osucc.Localisation;
using osucc.Plugin;
using osucc.UI.Overlays;
using osuTK;
using osuTK.Graphics;
using System.Diagnostics;
using System.IO;
using Markdig.Syntax.Inlines;
using osu.Framework.Graphics.Cursor;

namespace osucc.UI.Plugins
{
    /// <summary>
    /// Full-screen details card for a single plugin based on <see cref="OsuCcWaveOverlay"/>.
    /// Two columns: a narrower left one with the summary (icon, name, status), metadata rows,
    /// action buttons (repository, enable/disable, clear data, delete/cancel deletion) and the
    /// description; a wider right one with the plugin's settings subsection.
    /// </summary>
    public partial class PluginDetailsOverlay : OsuCcWaveOverlay
    {
        private readonly FillFlowContainer content;

        private SpriteIcon? fallbackIcon;
        private SettingsSubsection? settingsSubsection;

        private OsuSpriteText? statusText;
        private IconButton? repositoryButton;
        private IconButton? toggleButton;
        private IconButton? clearDataButton;
        private IconButton? deleteButton;
        private PluginEntry? displayedEntry;

        private static IReadOnlyDictionary<string, LocalisableString> localisedNames
            => PluginManager.Plugins.ToDictionary(e => e.Id, e => PluginCardLayout.LocalisedName(e));

        public PluginDetailsOverlay()
            : base(OverlayColourScheme.Green)
        {
            content = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 16),
                Padding = new MarginPadding
                {
                    Vertical = 20,
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Header.HeaderIcon = FontAwesome.Solid.PuzzlePiece;
            MainAreaContent.Add(content);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (displayedEntry != null)
                displayedEntry.StateChanged -= updateDisplayedEntryUi;

            base.Dispose(isDisposing);
        }

        /// <summary>Shows this overlay populated with the given plugin's information and settings.</summary>
        public void ShowPlugin(PluginEntry entry)
        {
            Show();

            if (!ReferenceEquals(displayedEntry, entry))
            {
                if (displayedEntry != null)
                    displayedEntry.StateChanged -= updateDisplayedEntryUi;

                displayedEntry = entry;
                entry.StateChanged += updateDisplayedEntryUi;
            }

            content.Clear();
            settingsSubsection = null;
            fallbackIcon = null;
            repositoryButton = null;
            toggleButton = null;
            clearDataButton = null;
            deleteButton = null;

            Header.TitleText = PluginCardLayout.LocalisedName(entry);

            string? authorSummary = entry.Authors.Count > 0 ? PluginCardLayout.FormatAuthorNames(entry) : null;
            Header.DescriptionText = LocalisableString.Format("{0} \u2022 v{1}", authorSummary ?? (LocalisableString)OsuCcStrings.UnknownAuthor, entry.Version);

            content.Add(new GridContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.4f),
                    new Dimension(GridSizeMode.Relative, 0.6f),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        createLeftColumn(entry),
                        createRightColumn(entry),
                    },
                },
            });

            if (fallbackIcon != null)
                fallbackIcon.Colour = ColourProvider.Content1;

            updateEntryUi(entry);
        }

        /// <summary>Builds the narrow left column: summary, metadata, actions and description boxes.</summary>
        private FillFlowContainer createLeftColumn(PluginEntry entry)
        {
            var nameById = localisedNames;
            var unavailableIds = PluginManager.Plugins.Where(e => !e.Enabled).Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
            var usedBy = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var plugin in PluginManager.Plugins)
            {
                foreach (string depId in plugin.Dependencies)
                {
                    if (!usedBy.TryGetValue(depId, out var dependentList))
                        usedBy[depId] = dependentList = new List<string>();

                    dependentList.Add(plugin.Id);
                }
            }

            var children = new List<Drawable>
            {
                CreateSummarySection(entry),
                CreateMetadataSection(entry, nameById, unavailableIds, usedBy),
                CreateActionsSection(entry),
            };

            if (!string.IsNullOrEmpty(entry.Description))
                children.Add(CreateDescriptionSection(entry));

            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 16),
                Padding = new MarginPadding { Right = 16 },
                Children = children,
            };
        }

        private DetailsSection CreateSummarySection(PluginEntry entry)
        {
            var icon = PluginCardLayout.CreateIcon(entry, 48, out fallbackIcon);

            statusText = new OsuSpriteText
            {
                Text = PluginCardLayout.StatusText(entry),
                Font = OsuFont.Default.With(size: 13),
            };

            return new DetailsSection
            {
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(16, 0),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = 48,
                            Child = icon,
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 4),
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = PluginCardLayout.LocalisedName(entry),
                                    Font = OsuFont.Torus.With(size: 22, weight: FontWeight.Bold),
                                },
                                statusText,
                            },
                        },
                    },
                },
            };
        }

        private static DetailsSection CreateMetadataSection(
            PluginEntry entry,
            IReadOnlyDictionary<string, LocalisableString> nameById,
            IReadOnlySet<string> unavailableIds,
            Dictionary<string, List<string>> usedBy)
        {
            var rows = new List<Drawable>
            {
                CreateMetadataRow(PluginsOverlayStrings.DetailsId, entry.Id),
                CreateMetadataRow(PluginsOverlayStrings.DetailsAuthor, PluginCardLayout.CreateAuthorValue(entry)),
                CreateMetadataRow(PluginsOverlayStrings.DetailsTags, PluginCardLayout.CreateTagsValue(entry)),
                CreateMetadataRow(PluginsOverlayStrings.DetailsVersion, entry.Version),
                CreateMetadataRow(PluginsOverlayStrings.DetailsApiVersion, entry.ApiVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                CreateMetadataRow(PluginsOverlayStrings.DetailsPriority, entry.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                CreateMetadataRow(PluginsOverlayStrings.DependenciesCaption, CreateDependenciesValue(entry, nameById, unavailableIds)),
                CreateMetadataRow(PluginsOverlayStrings.UsedByCaption, CreateUsedByValue(entry, nameById, usedBy)),
            };

            return new DetailsSection
            {
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 6),
                    Children = rows,
                },
            };
        }

        private DetailsSection CreateActionsSection(PluginEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.Repository))
            {
                repositoryButton = PluginCardLayout.CreateActionButton(FontAwesome.Brands.Github, PluginsOverlayStrings.OpenRepository, () => openRepository(entry.Repository!));
            }

            toggleButton = PluginCardLayout.CreateActionButton(FontAwesome.Solid.ToggleOn, PluginsOverlayStrings.ToggleDisabled, () => toggleEnabled(entry));
            clearDataButton = PluginCardLayout.CreateActionButton(FontAwesome.Solid.Eraser, PluginsOverlayStrings.ClearDataTitle, () => clearPluginData(entry));
            deleteButton = PluginCardLayout.CreateActionButton(FontAwesome.Solid.Trash, PluginsOverlayStrings.DeletePluginTooltip, () => confirmDelete(entry));

            var buttons = new List<Drawable>();

            if (repositoryButton != null)
                buttons.Add(repositoryButton);

            buttons.Add(toggleButton);
            buttons.Add(clearDataButton);
            buttons.Add(deleteButton);

            return new DetailsSection
            {
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 10),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = PluginsOverlayStrings.DetailsActionsTitle,
                            Font = OsuFont.Torus.With(size: 18, weight: FontWeight.Bold),
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(8, 0),
                            Children = buttons,
                        },
                    },
                },
            };
        }

        private static void toggleEnabled(PluginEntry entry)
        {
            if (entry.PendingDelete)
                return;

            bool enabled = !entry.Enabled;

            PluginManager.SetPluginEnabled(entry.Id, enabled);

            ClientNotifications.Info(enabled
                ? PluginsOverlayStrings.PluginEnabled(PluginCardLayout.LocalisedName(entry))
                : PluginsOverlayStrings.PluginDisabled(PluginCardLayout.LocalisedName(entry)));
        }

        private static void clearPluginData(PluginEntry entry)
        {
            if (entry.PendingDelete)
                return;

            LocalisableString name = PluginCardLayout.LocalisedName(entry);

            if (!ClientDialogs.Confirm(
                    PluginsOverlayStrings.ClearDataTitle,
                    PluginsOverlayStrings.ClearDataBody(name),
                    () =>
                    {
                        PluginManager.ClearPluginData(entry.Id);
                        ClientNotifications.Info(PluginsOverlayStrings.ClearDataConfirmed(name));
                    }))
            {
                ClientNotifications.Error(PluginsOverlayStrings.ConfirmDialogFailed);
            }
        }

        private static void confirmDelete(PluginEntry entry)
        {
            LocalisableString name = PluginCardLayout.LocalisedName(entry);

            if (!ClientDialogs.Confirm(
                    PluginsOverlayStrings.DeleteTitle,
                    PluginsOverlayStrings.DeleteBody(name),
                    () =>
                    {
                        PluginManager.RemovePlugin(entry.Id);
                        ClientNotifications.Info(PluginsOverlayStrings.DeleteConfirmed(name));
                    }))
            {
                ClientNotifications.Error(PluginsOverlayStrings.ConfirmDialogFailed);
            }
        }

        private static void cancelDelete(PluginEntry entry)
        {
            PluginManager.RestorePlugin(entry.Id);
            ClientNotifications.Info(PluginsOverlayStrings.DeleteRestored(PluginCardLayout.LocalisedName(entry)));
        }

        /// <summary>Repaints the status line and action buttons when the displayed plugin's state changes.</summary>
        private void updateDisplayedEntryUi() => updateEntryUi(displayedEntry!);

        private static void openRepository(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PluginDetailsOverlay: could not open repository '{url}': {ex}");
                ClientNotifications.Error(PluginsOverlayStrings.RepositoryOpenFailed(url));
            }
        }

        /// <summary>Refreshes the status line and the action buttons from the plugin's current state, without rebuilding the settings section.</summary>
        private void updateEntryUi(PluginEntry entry)
        {
            if (statusText != null)
            {
                statusText.Text = PluginCardLayout.StatusText(entry);
                statusText.Colour = PluginCardLayout.StatusColour(entry.Status);
            }

            bool interactive = !entry.PendingDelete;

            if (repositoryButton != null)
            {
                repositoryButton.Enabled.Value = interactive;
                repositoryButton.FadeTo(interactive ? 1 : 0.35f, 100);
            }

            if (toggleButton != null)
            {
                toggleButton.Icon = entry.Enabled ? FontAwesome.Solid.ToggleOn : FontAwesome.Solid.ToggleOff;
                toggleButton.TooltipText = entry.Enabled ? PluginsOverlayStrings.ToggleDisabled : PluginsOverlayStrings.ToggleEnabled;
                toggleButton.IconColour = entry.Enabled ? OsuCcColours.Success : Color4.White;
                toggleButton.Enabled.Value = interactive;
                toggleButton.FadeTo(interactive ? 1 : 0.35f, 100);
            }

            if (clearDataButton != null)
            {
                clearDataButton.Enabled.Value = interactive;
                clearDataButton.FadeTo(interactive ? 1 : 0.35f, 100);
            }

            if (deleteButton == null)
                return;

            if (entry.PendingDelete)
            {
                deleteButton.Icon = FontAwesome.Solid.Undo;
                deleteButton.TooltipText = PluginsOverlayStrings.CancelDelete;
                deleteButton.Action = () => cancelDelete(entry);
                deleteButton.IconColour = OsuCcColours.Info;
            }
            else
            {
                deleteButton.Icon = FontAwesome.Solid.Trash;
                deleteButton.TooltipText = PluginsOverlayStrings.DeletePluginTooltip;
                deleteButton.Action = () => confirmDelete(entry);
                deleteButton.IconColour = OsuCcColours.Error;
            }
        }

        /// <summary>
        /// "Depends on" value for the metadata table: clickable dependency links, or a dimmed
        /// placeholder when the plugin has no dependencies.
        /// </summary>
        private static Drawable CreateDependenciesValue(
            PluginEntry entry,
            IReadOnlyDictionary<string, LocalisableString> nameById,
            IReadOnlySet<string> unavailableIds)
            => entry.Dependencies.Count > 0
                ? PluginCardLayout.CreateDependenciesValue(entry.Dependencies, nameById, unavailableIds)
                : CreatePlaceholderValue(PluginsOverlayStrings.DetailsRelationsNone);

        /// <summary>
        /// "Used by" value for the metadata table: clickable dependent links, or a dimmed
        /// placeholder when no other plugin depends on this one.
        /// </summary>
        private static Drawable CreateUsedByValue(
            PluginEntry entry,
            IReadOnlyDictionary<string, LocalisableString> nameById,
            Dictionary<string, List<string>> usedBy)
            => usedBy.TryGetValue(entry.Id, out var dependents)
                ? PluginCardLayout.CreateUsedByValue(dependents, nameById)
                : CreatePlaceholderValue(PluginsOverlayStrings.DetailsRelationsNone);

        private static OsuSpriteText CreatePlaceholderValue(LocalisableString text) => new()
        {
            Text = text,
            Font = OsuFont.Default.With(size: 13),
            Colour = Color4.White.Opacity(0.45f),
        };

        private static FillFlowContainer CreateMetadataRow(LocalisableString label, LocalisableString value)
            => CreateMetadataRow(label, new OsuSpriteText
            {
                Text = value,
                Font = OsuFont.Default.With(size: 13),
                Colour = Color4.White,
            });

        private static FillFlowContainer CreateMetadataRow(LocalisableString label, Drawable value) => new()
        {
            AutoSizeAxes = Axes.Both,
            Direction = FillDirection.Horizontal,
            Spacing = new Vector2(8, 0),
            Children = new Drawable[]
            {
                new OsuSpriteText
                {
                    Text = label,
                    Width = 120,
                    Font = OsuFont.Default.With(size: 13),
                    Colour = Color4.White.Opacity(0.55f),
                },
                value,
            },
        };

        private static DetailsSection CreateDescriptionSection(PluginEntry entry)
        {
            var flow = new TextFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
            };

            flow.AddText(
                PluginCardLayout.Description(entry),
                s =>
                {
                    s.Font = OsuFont.Default.With(size: 13);
                    s.Colour = Color4.White.Opacity(0.7f);
                });

            return new DetailsSection { Child = flow };
        }

        private DetailsRightPane createRightColumn(PluginEntry entry)
            => new DetailsRightPane(entry, () => createSettingsContent(entry), doc => createDocumentContent(entry, doc));

        private Drawable createSettingsContent(PluginEntry entry)
        {
            var factory = PluginManager.GetSettingsSubsectionFactory(entry.Id);

            if (factory == null)
            {
                return new OsuSpriteText
                {
                    Text = PluginsOverlayStrings.NoPluginSettings,
                    Font = OsuFont.Default.With(size: 13),
                    Colour = Color4.White.Opacity(0.55f),
                };
            }

            try
            {
                settingsSubsection = factory();
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PluginDetailsOverlay: failed to create settings for '{entry.Id}': {ex}");
                return new OsuSpriteText
                {
                    Text = PluginsOverlayStrings.SettingsOpenFailed(ex.Message),
                    Font = OsuFont.Default.With(size: 13),
                    Colour = OsuCcColours.Error,
                };
            }

            return settingsSubsection;
        }

        private static Drawable createDocumentContent(PluginEntry entry, PluginDocument doc)
        {
            string fullPath = Path.Combine(entry.Directory, doc.Path.Replace('\\', '/'));

            if (!File.Exists(fullPath))
            {
                string resPath = Path.Combine(entry.Directory, "res", doc.Path.Replace('\\', '/'));
                if (File.Exists(resPath))
                    fullPath = resPath;
                else
                {
                    string rootFileName = Path.Combine(entry.Directory, Path.GetFileName(doc.Path));
                    if (File.Exists(rootFileName))
                        fullPath = rootFileName;
                }
            }

            if (!File.Exists(fullPath))
            {
                return new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 8),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = doc.Title,
                            Font = OsuFont.Torus.With(size: 18, weight: FontWeight.Bold),
                        },
                        new OsuSpriteText
                        {
                            Text = PluginsOverlayStrings.DocumentFileNotFound(doc.Path),
                            Font = OsuFont.Default.With(size: 13),
                            Colour = OsuCcColours.Warning,
                        },
                    },
                };
            }

            try
            {
                string markdown = File.ReadAllText(fullPath);

                return new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 12),
                    Children = new Drawable[]
                    {
                        new PluginMarkdownContainer(entry.Directory)
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Text = markdown,
                        },
                    },
                };
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PluginDetailsOverlay: failed to read document '{fullPath}': {ex}");
                return new OsuSpriteText
                {
                    Text = ex.Message,
                    Font = OsuFont.Default.With(size: 13),
                    Colour = OsuCcColours.Error,
                };
            }
        }

        /// <summary>Markdown container resolving local plugin resources for embedded images.</summary>
        private sealed partial class PluginMarkdownContainer : OsuMarkdownContainer
        {
            private readonly string baseDirectory;

            public PluginMarkdownContainer(string baseDirectory)
            {
                this.baseDirectory = baseDirectory;
            }

            public override OsuMarkdownTextFlowContainer CreateTextFlow() => new PluginMarkdownTextFlowContainer(baseDirectory);

            private sealed partial class PluginMarkdownTextFlowContainer : OsuMarkdownTextFlowContainer
            {
                private readonly string baseDirectory;

                public PluginMarkdownTextFlowContainer(string baseDirectory)
                {
                    this.baseDirectory = baseDirectory;
                }

                protected override void AddImage(LinkInline linkInline)
                {
                    if (linkInline.Url != null && (linkInline.Url.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) || linkInline.Url.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase)))
                    {
                        base.AddImage(linkInline);
                        return;
                    }

                    AddDrawable(new PluginLocalMarkdownImage(baseDirectory, linkInline));
                }
            }

            private sealed partial class PluginLocalMarkdownImage : CompositeDrawable, IHasTooltip
            {
                public LocalisableString TooltipText { get; }

                private readonly string baseDirectory;
                private readonly LinkInline linkInline;

                public PluginLocalMarkdownImage(string baseDirectory, LinkInline linkInline)
                {
                    this.baseDirectory = baseDirectory;
                    this.linkInline = linkInline;
                    TooltipText = linkInline.Title ?? string.Empty;
                    AutoSizeAxes = Axes.Both;
                }

                [BackgroundDependencyLoader]
                private void load()
                {
                    string rawUrl = linkInline.Url ?? string.Empty;
                    string relPath = rawUrl.Replace('\\', '/');

                    string fullPath = Path.Combine(baseDirectory, relPath);
                    if (!File.Exists(fullPath))
                    {
                        string resPath = Path.Combine(baseDirectory, "res", relPath);
                        if (File.Exists(resPath))
                            fullPath = resPath;
                        else
                        {
                            string fileName = Path.Combine(baseDirectory, Path.GetFileName(relPath));
                            if (File.Exists(fileName))
                                fullPath = fileName;
                        }
                    }

                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            using var stream = File.OpenRead(fullPath);
                            var texture = TextureHelper.FromStream(stream);
                            if (texture != null)
                            {
                                var sprite = new Sprite
                                {
                                    Texture = texture,
                                };

                                if (texture.DisplayWidth > 640)
                                {
                                    float scale = 640f / texture.DisplayWidth;
                                    sprite.Size = new Vector2(640, texture.DisplayHeight * scale);
                                    sprite.FillMode = FillMode.Fit;
                                }

                                InternalChild = sprite;
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            TimingLog.Error($"PluginLocalMarkdownImage: failed to load '{fullPath}': {ex}");
                        }
                    }

                    InternalChild = new OsuSpriteText
                    {
                        Text = $"[Image: {rawUrl}]",
                        Colour = OsuCcColours.Warning,
                        Font = OsuFont.Default.With(size: 12),
                    };
                }
            }
        }

        /// <summary>Tabbed container hosting Settings and Markdown documents on the right pane.</summary>
        private sealed partial class DetailsRightPane : CompositeDrawable
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            private readonly PluginEntry entry;
            private readonly Func<Drawable> createSettings;
            private readonly Func<PluginDocument, Drawable> createDoc;
            private readonly Container contentHolder;
            private readonly List<TabButton> tabButtons = new();

            public DetailsRightPane(PluginEntry entry, Func<Drawable> createSettings, Func<PluginDocument, Drawable> createDoc)
            {
                this.entry = entry;
                this.createSettings = createSettings;
                this.createDoc = createDoc;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                var tabsFlow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(8, 0),
                    Margin = new MarginPadding { Bottom = 10 },
                };

                // Settings tab button
                var settingsTab = new TabButton(FontAwesome.Solid.Cog, PluginsOverlayStrings.DetailsSettingsTitle, () => selectTab(0));
                tabButtons.Add(settingsTab);
                tabsFlow.Add(settingsTab);

                // Document tab buttons
                for (int i = 0; i < entry.Documents.Count; i++)
                {
                    int docIndex = i;
                    var doc = entry.Documents[i];
                    var icon = PluginCardLayout.ResolveFontAwesomeIcon(doc.IconGlyph) ?? FontAwesome.Solid.FileAlt;
                    var docTab = new TabButton(icon, doc.Title, () => selectTab(docIndex + 1));
                    tabButtons.Add(docTab);
                    tabsFlow.Add(docTab);
                }

                InternalChild = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        tabsFlow,
                        new DetailsSection
                        {
                            Child = contentHolder = new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                            },
                        },
                    },
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                selectTab(0);
            }

            private void selectTab(int index)
            {
                for (int i = 0; i < tabButtons.Count; i++)
                    tabButtons[i].Active = (i == index);

                contentHolder.Clear();

                if (index == 0)
                    contentHolder.Child = createSettings();
                else
                {
                    var doc = entry.Documents[index - 1];
                    contentHolder.Child = createDoc(doc);
                }
            }

            private sealed partial class TabButton : ClickableContainer
            {
                [Resolved]
                private OverlayColourProvider colourProvider { get; set; } = null!;

                private readonly Box background;
                private readonly SpriteIcon iconSprite;
                private readonly OsuSpriteText textSprite;

                private bool active;

                public bool Active
                {
                    get => active;
                    set
                    {
                        if (active == value)
                            return;

                        active = value;
                        updateState();
                    }
                }

                public TabButton(IconUsage icon, LocalisableString title, Action onClick)
                {
                    Action = onClick;
                    AutoSizeAxes = Axes.Both;
                    Masking = true;
                    CornerRadius = 6;

                    InternalChildren = new Drawable[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Transparent,
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(8, 0),
                            Padding = new MarginPadding { Horizontal = 14, Vertical = 8 },
                            Children = new Drawable[]
                            {
                                iconSprite = new SpriteIcon
                                {
                                    Size = new Vector2(14),
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Icon = icon,
                                },
                                textSprite = new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Text = title,
                                    Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                                },
                            },
                        },
                    };
                }

                [BackgroundDependencyLoader]
                private void load()
                {
                    updateState();
                }

                protected override bool OnHover(osu.Framework.Input.Events.HoverEvent e)
                {
                    if (!active)
                        background.FadeColour(colourProvider.Background3.Opacity(0.5f), 100);
                    return base.OnHover(e);
                }

                protected override void OnHoverLost(osu.Framework.Input.Events.HoverLostEvent e)
                {
                    if (!active)
                        background.FadeColour(Color4.Transparent, 100);
                    base.OnHoverLost(e);
                }

                private void updateState()
                {
                    if (colourProvider == null)
                        return;

                    if (active)
                    {
                        background.FadeColour(colourProvider.Background3, 150);
                        iconSprite.FadeColour(colourProvider.Colour1, 150);
                        textSprite.FadeColour(Color4.White, 150);
                    }
                    else
                    {
                        background.FadeColour(Color4.Transparent, 150);
                        iconSprite.FadeColour(Color4.White.Opacity(0.6f), 150);
                        textSprite.FadeColour(Color4.White.Opacity(0.6f), 150);
                    }
                }
            }
        }

        /// <summary>A rounded container grouping one block of the details card.</summary>
        private sealed partial class DetailsSection : Container
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            private readonly Container contentContainer;

            private Box background = null!;

            /// <summary>
            /// Routed content (e.g. via <see cref="Child"/>) lands in <see cref="contentContainer"/>
            /// so it is padded away from the box edge while the background fills the whole masked area.
            /// </summary>
            protected override Container<Drawable> Content => contentContainer;

            public DetailsSection()
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                Masking = true;
                CornerRadius = 10;

                EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                {
                    Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
                    Colour = Color4.Black.Opacity(0.15f),
                    Radius = 4,
                    Offset = new Vector2(0, 2),
                };

                AddInternal(contentContainer = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding
                    {
                        Horizontal = 20,
                        Vertical = 16,
                    },
                });
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AddInternal(background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Depth = float.MaxValue,
                    Colour = colourProvider.Background4,
                });
            }
        }
    }
}
