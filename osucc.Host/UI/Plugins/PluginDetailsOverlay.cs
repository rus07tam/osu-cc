using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osucc.Core;
using osucc.Localisation;
using osucc.Plugin;
using osucc.UI.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osucc.UI.Plugins
{
    /// <summary>
    /// Full-screen details card for a single plugin: summary (icon, name, status), full metadata
    /// (id, author, version, API version, priority), description, dependency relations and the
    /// plugin's settings subsection. Opened by clicking a plugin card or a
    /// <see cref="PluginNameLink"/>. Built on <see cref="OsuCcShearedOverlay"/> so only one
    /// osu!cc overlay stays visible at a time ("last opened wins").
    /// </summary>
    public partial class PluginDetailsOverlay : OsuCcShearedOverlay
    {
        private readonly FillFlowContainer content;

        private SpriteIcon? fallbackIcon;
        private SettingsSubsection? settingsSubsection;

        private static IReadOnlyDictionary<string, LocalisableString> localisedNames
            => PluginManager.Plugins.ToDictionary(e => e.Id, e => OsuCcLocalisation.Get($"{e.Id}:name", e.Name));

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
                    Horizontal = Padding * 2,
                    Vertical = Padding,
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            MainAreaContent.Add(new OverlayScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = content,
            });
        }

        /// <summary>Shows this overlay populated with the given plugin's information and settings.</summary>
        public void ShowPlugin(PluginEntry entry)
        {
            Show();

            content.Clear();
            settingsSubsection = null;
            fallbackIcon = null;

            Header.Title = OsuCcLocalisation.Get($"{entry.Id}:name", entry.Name);

            string? authorSummary = entry.Authors.Count > 0 ? PluginCardLayout.FormatAuthorNames(entry) : null;
            Header.Description = LocalisableString.Format("{0} \u2022 v{1}", authorSummary ?? (LocalisableString)OsuCcStrings.UnknownAuthor, entry.Version);

            content.Add(CreateSummarySection(entry));

            if (fallbackIcon != null)
                fallbackIcon.Colour = ColourProvider.Content1;

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

            content.Add(CreateMetadataSection(entry, nameById, unavailableIds, usedBy));

            if (!string.IsNullOrEmpty(entry.Description))
                content.Add(CreateDescriptionSection(entry));

            content.Add(CreateSettingsSection(entry));
        }

        private DetailsSection CreateSummarySection(PluginEntry entry)
        {
            var icon = PluginCardLayout.CreateIcon(entry, 48, out fallbackIcon);

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
                                    Text = OsuCcLocalisation.Get($"{entry.Id}:name", entry.Name),
                                    Font = OsuFont.Torus.With(size: 22, weight: FontWeight.Bold),
                                },
                                new OsuSpriteText
                                {
                                    Text = getStatusText(entry),
                                    Font = OsuFont.Default.With(size: 13),
                                    Colour = getStatusColour(entry.Status),
                                },
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
                CreateMetadataRow(PluginsOverlayStrings.DetailsTags, PluginCardLayout.CreateTagsValue(entry, tag => PluginsOverlayComponent.Instance?.SearchTag(tag))),
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

        private static FillFlowContainer CreateMetadataRow(LocalisableString label, LocalisableString value) => new()
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
                new OsuSpriteText
                {
                    Text = value,
                    Font = OsuFont.Default.With(size: 13),
                    Colour = Color4.White,
                },
            },
        };

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

        private static DetailsSection CreateDescriptionSection(PluginEntry entry) => new()
        {
            Child = new OsuSpriteText
            {
                RelativeSizeAxes = Axes.X,
                Text = OsuCcLocalisation.Get($"{entry.Id}:description", entry.Description ?? string.Empty),
                Font = OsuFont.Default.With(size: 13),
                Colour = Color4.White.Opacity(0.7f),
            },
        };

        private DetailsSection CreateSettingsSection(PluginEntry entry)
        {
            var section = new DetailsSection();

            var factory = PluginManager.GetSettingsSubsectionFactory(entry.Id);

            if (factory == null)
            {
                section.Child = new OsuSpriteText
                {
                    Text = PluginsOverlayStrings.NoPluginSettings,
                    Font = OsuFont.Default.With(size: 13),
                    Colour = Color4.White.Opacity(0.55f),
                };
                return section;
            }

            try
            {
                settingsSubsection = factory();
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PluginDetailsOverlay: failed to create settings for '{entry.Id}': {ex}");
                section.Child = new OsuSpriteText
                {
                    Text = PluginsOverlayStrings.SettingsOpenFailed(ex.Message),
                    Font = OsuFont.Default.With(size: 13),
                    Colour = OsuCcColours.Error,
                };
                return section;
            }

            section.Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 12),
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = PluginsOverlayStrings.DetailsSettingsTitle,
                        Font = OsuFont.Torus.With(size: 18, weight: FontWeight.Bold),
                    },
                    settingsSubsection,
                },
            };

            return section;
        }

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

        /// <summary>A rounded container grouping one block of the details card.</summary>
        private sealed partial class DetailsSection : Container
        {
            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            private readonly Box background;

            public DetailsSection()
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                Masking = true;
                CornerRadius = 8;
                Padding = new MarginPadding
                {
                    Horizontal = 20,
                    Vertical = 16,
                };

                InternalChildren = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                background.Colour = colourProvider.Background4;
            }
        }
    }
}
