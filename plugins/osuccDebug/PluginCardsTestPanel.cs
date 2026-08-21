using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osucc.Client;
using osucc.Localisation;
using osucc.Plugin;
using osucc.UI.Plugins;
using osuTK;
using osuTK.Graphics;

namespace osuccDebug
{
    /// <summary>
    /// Debug panel displaying preview <see cref="PluginCard"/> instances with all possible
    /// <see cref="PluginStatus"/> lifecycle states (Active, PendingEnable, PendingDisable,
    /// Disabled, PendingDelete, and Error).
    /// </summary>
    public partial class PluginCardsTestPanel : FillFlowContainer
    {
        private readonly IOsuCcPluginHost host;
        private readonly List<PluginCard> cards = new();
        private readonly FillFlowContainer cardsFlow;

        public PluginCardsTestPanel(IOsuCcPluginHost host)
        {
            this.host = host;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, 12);

            Children = new Drawable[]
            {
                new OsuSpriteText
                {
                    Text = osuccDebugStrings.PluginCardsSectionSubtitle,
                    Font = OsuFont.Default.With(size: 13),
                    Colour = Color4.White.Opacity(0.7f),
                },
                cardsFlow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 8),
                },
            };

            populateMockCards();
        }

        private void populateMockCards()
        {
            var mockEntries = new[]
            {
                // 1. Active: Loaded & Enabled
                new PluginEntry
                {
                    Id = "custom-user-groups",
                    Name = "Custom User Groups",
                    Version = "1.2.0",
                    Authors = new[]
                    {
                        new PluginAuthor("rus07tam", 33701908),
                        new PluginAuthor("ppy", 2),
                    },
                    Tags = new[] { "groups", "ui", "visuals", "roles" },
                    Description = "Custom user groups and badges styling for leaderboard and profile screens.",
                    Icon = "Users",
                    Plugin = new MockPlugin(),
                    Enabled = true,
                },

                // 2. PendingEnable: Enabled but not loaded yet (waiting for restart)
                new PluginEntry
                {
                    Id = "username-visuals",
                    Name = "Username Visuals",
                    Version = "1.0.1",
                    Authors = new[]
                    {
                        new PluginAuthor("rus07tam", 33701908),
                    },
                    Tags = new[] { "visuals", "chat" },
                    Description = "Gives supporters animated glowing and gradient username visuals.",
                    Icon = "PaintBrush",
                    Plugin = null,
                    Enabled = true,
                },

                // 3. PendingDisable: Loaded but disabled during this session
                new PluginEntry
                {
                    Id = "fake-supporter",
                    Name = "Fake Supporter",
                    Version = "2.0.0",
                    Authors = new[]
                    {
                        new PluginAuthor("osu-cc team"),
                    },
                    Tags = new[] { "supporter", "perks" },
                    Description = "Unlocks client-side supporter tag features and visual perks.",
                    Icon = "Heart",
                    Plugin = new MockPlugin(),
                    Enabled = false,
                },

                // 4. Disabled: Not loaded and disabled
                new PluginEntry
                {
                    Id = "friends-leaderboard",
                    Name = "Friends Leaderboard",
                    Version = "1.0.0",
                    Authors = new[]
                    {
                        new PluginAuthor("rus07tam", 33701908),
                    },
                    Tags = new[] { "leaderboard", "gameplay" },
                    Description = "Displays friend rankings directly on song select without opening profile.",
                    Icon = "UserFriends",
                    Plugin = null,
                    Enabled = false,
                },

                // 5. PendingDelete: Marked for removal on next launch
                new PluginEntry
                {
                    Id = "example-plugin",
                    Name = "Example Plugin",
                    Version = "0.9.5",
                    Authors = new[]
                    {
                        new PluginAuthor("ExampleDev"),
                    },
                    Tags = new[] { "example", "template" },
                    Description = "Demonstration template plugin marked for deletion on next launch.",
                    Icon = "PuzzlePiece",
                    Plugin = new MockPlugin(),
                    Enabled = false,
                    PendingDelete = true,
                },

                // 6. Error: Discovery / injection threw an exception
                new PluginEntry
                {
                    Id = "corrupted-plugin",
                    Name = "Corrupted Hook Plugin",
                    Version = "0.1.0-alpha",
                    Authors = new[]
                    {
                        new PluginAuthor("FaultyDev"),
                    },
                    Tags = new[] { "unstable", "hook" },
                    Description = "Plugin that threw an exception during assembly patch injection.",
                    Icon = "ExclamationTriangle",
                    LoadError = new InvalidOperationException("Could not find target method DrawFrame() on PlayerLoader"),
                    Enabled = true,
                },
            };

            foreach (var entry in mockEntries)
            {
                var card = new PluginCard(entry, moveCard);

                card.EnabledChanged = (c, isEnabled) =>
                {
                    c.Entry.Enabled = isEnabled;
                    host.Notify($"Mock card '{c.Entry.Name}': Enabled = {isEnabled} (Status: {c.Entry.Status})", NotificationKind.Info);
                };

                card.Clicked = c =>
                {
                    host.Notify($"Mock card clicked: '{c.Entry.Name}' (Status: {c.Entry.Status})", NotificationKind.Info);
                };

                cards.Add(card);
                cardsFlow.Add(card);
            }

            applyOrder();
        }

        private void moveCard(PluginCard card, int delta)
        {
            int index = cards.IndexOf(card);
            int target = index + delta;

            if (index < 0 || target < 0 || target >= cards.Count)
                return;

            (cards[index], cards[target]) = (cards[target], cards[index]);
            applyOrder();

            host.Notify($"Moved '{card.Entry.Name}' to position {target + 1}", NotificationKind.Info);
        }

        private void applyOrder()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].UpdateMoveAvailability(i, cards.Count);

                if (cardsFlow.Contains(cards[i]))
                    cardsFlow.SetLayoutPosition(cards[i], i);
            }
        }

        private sealed class MockPlugin : OsuCcPlugin
        {
            protected override void OnLoad()
            {
            }
        }
    }
}
