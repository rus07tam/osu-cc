using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osucc.Client;
using osucc.Localisation;
using osucc.Plugin;
using osucc.UI.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osucc.UI.Plugins
{
    public enum PluginsOverlaySection
    {
        [LocalisableDescription(typeof(PluginsOverlayStrings), nameof(PluginsOverlayStrings.InstalledTab))]
        Installed,

        [LocalisableDescription(typeof(PluginsOverlayStrings), nameof(PluginsOverlayStrings.BrowserTab))]
        Browser,
    }

    /// <summary>
    /// Full-screen wave overlay managing plugins (Installed tab with multi-keyword search,
    /// card view and reordering, and Browser tab with a catalog preview).
    /// </summary>
    public partial class PluginsOverlay : OsuCcWaveOverlay
    {
        private readonly FillFlowContainer list;
        private readonly List<PluginCard> cards = new();
        private readonly Bindable<string> filter = new(string.Empty);
        private readonly Bindable<PluginsOverlaySection> currentSection = new(PluginsOverlaySection.Installed);

        private FillFlowContainer installedContent = null!;
        private Container browserContent = null!;
        private SearchTextBox searchBox = null!;
        private OsuSpriteText? noResultsText;
        private PluginsTabControl tabControl = null!;

        public PluginsOverlay()
            : base(OverlayColourScheme.Green)
        {
            list = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Header.TitleText = PluginsOverlayStrings.OverlayTitle;
            Header.DescriptionText = PluginsOverlayStrings.OverlayDescription;
            Header.HeaderIcon = FontAwesome.Solid.PuzzlePiece;

            tabControl = new PluginsTabControl
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft,
            };
            tabControl.Current.BindTo(currentSection);

            Header.ContentRow.Add(tabControl);

            MainAreaContent.Add(new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    installedContent = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 16),
                        Padding = new MarginPadding { Top = 16 },
                        Children = new Drawable[]
                        {
                            searchBox = new SearchTextBox
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                RelativeSizeAxes = Axes.X,
                                Width = 0.45f,
                                PlaceholderText = PluginsOverlayStrings.SearchPlaceholder,
                                Current = filter,
                            },
                            list,
                        },
                    },
                    browserContent = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Alpha = 0,
                        Padding = new MarginPadding { Vertical = 40 },
                        Child = createBrowserStub(),
                    },
                },
            });

            currentSection.BindValueChanged(e =>
            {
                if (e.NewValue == PluginsOverlaySection.Installed)
                {
                    installedContent.FadeIn(200, Easing.OutQuint);
                    browserContent.FadeOut(200, Easing.OutQuint);
                }
                else
                {
                    installedContent.FadeOut(200, Easing.OutQuint);
                    browserContent.FadeIn(200, Easing.OutQuint);

                    if (!browserInitialized)
                    {
                        browserInitialized = true;
                        loadBrowserPage();
                    }
                }
            }, true);

            filter.BindValueChanged(e => applyFilter(e.NewValue), true);
        }

        private FillFlowContainer browserList = null!;
        private int browserPage;
        private bool browserLoading;
        private bool browserInitialized;

        private LoadingSpinner browserSpinner = null!;

        private FillFlowContainer createBrowserStub()
        {
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 16),
                Padding = new MarginPadding { Horizontal = 40 },
                Children = new Drawable[]
                {
                    browserList = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 10),
                    },
                    browserSpinner = new LoadingSpinner(true)
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Margin = new MarginPadding { Vertical = 10 },
                    },
                    new ShearedButton
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Width = 200,
                        Text = "Load More",
                        Action = loadBrowserPage,
                    }
                }
            };
        }

        private void loadBrowserPage()
        {
            if (browserLoading) return;
            browserLoading = true;

            browserSpinner.Show();

            Task.Run(async () =>
            {
                var service = PluginBrowserService.Instance;
                if (service == null)
                {
                    Schedule(() => { browserSpinner.Hide(); browserLoading = false; });
                    return;
                }

                var results = await service.GetPluginsAsync(++browserPage).ConfigureAwait(false);

                Schedule(() =>
                {
                    browserLoading = false;
                    browserSpinner.Hide();
                    foreach (var info in results)
                    {
                        var dummy = new PluginEntry
                        {
                            Id = info.Id,
                            Name = info.Name,
                            Description = info.Description,
                            Version = info.Version,
                            Icon = info.Icon,
                            IconPath = info.IconPath,
                            IconResource = info.IconResource,
                            Repository = info.Repository,
                            Authors = info.Authors,
                            Tags = info.Tags,
                            Documents = info.Documents,
                        };

                        var card = new PluginCard(dummy) { IsCatalogMode = true };
                        card.Clicked = _ => PluginsOverlayComponent.Instance?.ShowRemotePlugin(info);
                        browserList.Add(card);
                    }
                });
            });
        }

        private void applyFilter(string query)
        {
            list.Clear(false);
            noResultsText = null;

            var filtered = new List<PluginCard>();

            foreach (var card in cards)
            {
                if (matchesFilter(card.Entry, query))
                    filtered.Add(card);
            }

            foreach (var card in filtered)
                list.Add(card);

            applyOrder();

            if (filtered.Count == 0 && cards.Count > 0)
            {
                list.Add(noResultsText = new OsuSpriteText
                {
                    Text = PluginsOverlayStrings.SearchNoResults,
                    Font = OsuFont.Default.With(size: 14),
                    Colour = Color4.White.Opacity(0.6f),
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Margin = new MarginPadding { Top = 20 },
                });
            }
        }

        private static bool matchesFilter(PluginEntry entry, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            string[] keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var haystacks = new List<string> { entry.Name, entry.Id };

            foreach (var author in entry.Authors)
                haystacks.Add(author.Name);

            haystacks.AddRange(entry.Tags);

            return keywords.All(keyword =>
                haystacks.Any(s => s.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
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
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Margin = new MarginPadding { Top = 20 },
                });
                return;
            }

            foreach (var entry in entries)
            {
                var card = new PluginCard(entry, moveCard);
                card.EnabledChanged = (c, isEnabled) =>
                {
                    PluginManager.SetPluginEnabled(c.Entry.Id, isEnabled);
                };
                cards.Add(card);
            }

            applyFilter(filter.Value);
        }

        private void applyOrder()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].UpdateMoveAvailability(i, cards.Count);

                if (list.Contains(cards[i]))
                    list.SetLayoutPosition(cards[i], i);
            }
        }

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

        private sealed partial class PluginsTabControl : OverlayTabControl<PluginsOverlaySection>
        {
            private const float bar_height = 2;

            public PluginsTabControl()
            {
                RelativeSizeAxes = Axes.None;
                AutoSizeAxes = Axes.X;
                Anchor = Anchor.BottomLeft;
                Origin = Anchor.BottomLeft;
                Height = 47;
                BarHeight = bar_height;
            }

            protected override TabItem<PluginsOverlaySection> CreateTabItem(PluginsOverlaySection value) => new PluginsTabItem(value);

            protected override TabFillFlowContainer CreateTabFlow() => new TabFillFlowContainer
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                Direction = FillDirection.Horizontal,
            };

            private sealed partial class PluginsTabItem : OverlayTabItem
            {
                public PluginsTabItem(PluginsOverlaySection value)
                    : base(value)
                {
                    Text.Text = value.GetLocalisableDescription().ToLower();
                    Text.Font = OsuFont.GetFont(size: 14);
                    Text.Margin = new MarginPadding { Vertical = 16.5f };
                    Bar.Margin = new MarginPadding { Bottom = bar_height };
                }
            }
        }
    }
}
