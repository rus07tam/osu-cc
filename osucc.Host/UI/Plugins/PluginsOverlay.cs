using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
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
    /// <summary>
    /// Full-screen overlay listing every discovered plugin (icon, name, author, version,
    /// description, tags, load status), with a search box that filters the list by name, author,
    /// id or tag. Opened from the Specials settings section.
    /// </summary>
    public partial class PluginsOverlay : OsuCcShearedOverlay
    {
        private readonly FillFlowContainer list;
        private readonly List<PluginCard> cards = new();
        private readonly Bindable<string> filter = new(string.Empty);
        private OverlayScrollContainer scrollContainer = null!;
        private SearchTextBox searchBox = null!;
        private OsuSpriteText? noResultsText;

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
                    Bottom = Padding,
                },
            };
        }

        private const float searchRowHeight = 35 + Padding * 2;

        [BackgroundDependencyLoader]
        private void load()
        {
            Header.Title = PluginsOverlayStrings.OverlayTitle;
            Header.Description = PluginsOverlayStrings.OverlayDescription;

            MainAreaContent.Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    scrollContainer = new OverlayScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = searchRowHeight },
                        Child = list,
                    },
                    searchBox = new SearchTextBox
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.X,
                        Width = 0.45f,
                        Margin = new MarginPadding { Top = Padding },
                        PlaceholderText = PluginsOverlayStrings.SearchPlaceholder,
                        Current = filter,
                    },
                },
            });

            filter.BindValueChanged(e => applyFilter(e.NewValue), true);
        }

        /// <summary>
        /// Filters the plugin cards to those matching the query across their name, authors, id and
        /// tags; re-adds the matching cards to the list in their saved order.
        /// </summary>
        private void applyFilter(string query)
        {
            // Remove cards without disposing them so they can be re-added below; the card set is
            // owned by `cards` and stays alive between filter passes.
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

            if (filtered.Count == 0)
            {
                list.Add(noResultsText = new OsuSpriteText
                {
                    Text = PluginsOverlayStrings.SearchNoResults,
                    Font = OsuFont.Default.With(size: 14),
                    Colour = Color4.White.Opacity(0.6f),
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
                });
                return;
            }

            foreach (var entry in entries)
                cards.Add(new PluginCard(entry, moveCard));

            applyFilter(filter.Value);
        }

        /// <summary>Refreshes the list layout positions and the up/down availability of every card.</summary>
        private void applyOrder()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].UpdateMoveAvailability(i, cards.Count);

                if (list.Contains(cards[i]))
                    list.SetLayoutPosition(cards[i], i);
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
    }
}
