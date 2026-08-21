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
    /// <see cref="PluginStatus"/> lifecycle states and rich <see cref="PluginDiagnostic"/> records.
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

        private static readonly string[] custom_user_groups_tags = { "groups", "ui", "visuals", "roles" };
        private static readonly string[] rich_presence_tags = { "discord", "rpc", "social" };
        private static readonly string[] skin_customiser_tags = { "skins", "visuals" };
        private static readonly string[] subdivide_nations_tags = { "flags", "leaderboard", "subdivisions" };
        private static readonly string[] lazer_tweaks_tags = { "gameplay", "mods" };
        private static readonly string[] native_sound_engine_tags = { "audio", "native", "bass" };
        private static readonly string[] corrupted_plugin_tags = { "unstable", "hook" };
        private static readonly string[] username_visuals_tags = { "visuals", "chat" };
        private static readonly string[] fake_supporter_tags = { "supporter", "perks" };
        private static readonly string[] friends_leaderboard_tags = { "leaderboard", "gameplay" };
        private static readonly string[] example_plugin_tags = { "example", "template" };

        private void populateMockCards()
        {
            var mockEntries = new List<PluginEntry>();

            // 1. Active: Clean, no diagnostics
            mockEntries.Add(new PluginEntry
            {
                Id = "custom-user-groups",
                Name = "Custom User Groups",
                Version = "1.2.0",
                Authors = new[]
                {
                    new PluginAuthor("rus07tam", 33701908),
                    new PluginAuthor("ppy", 2),
                },
                Tags = custom_user_groups_tags,
                Description = "Custom user groups and badges styling for leaderboard and profile screens.",
                Icon = "Users",
                Plugin = new MockPlugin(),
                Enabled = true,
            });

            // 2. Active: 1 Notice record
            var noticeEntry = new PluginEntry
            {
                Id = "rich-presence",
                Name = "Discord Rich Presence",
                Version = "2.1.0",
                Authors = new[] { new PluginAuthor("DiscordDev") },
                Tags = rich_presence_tags,
                Description = "Displays detailed map and game status on your Discord profile.",
                Icon = "Gamepad",
                Plugin = new MockPlugin(),
                Enabled = true,
            };
            noticeEntry.AddDiagnostic(PluginDiagnostic.Notice("Optional integration with 'spotify-sync' is available", source: PluginDiagnosticSource.Dependency, target: "spotify-sync"));
            mockEntries.Add(noticeEntry);

            // 3. Active: 1 Warning record
            var warningEntry = new PluginEntry
            {
                Id = "skin-customiser",
                Name = "Skin Customiser",
                Version = "1.4.0",
                Authors = new[] { new PluginAuthor("SkinMaster") },
                Tags = skin_customiser_tags,
                Description = "Customise hitcircles, cursors and sound effects per beatmap.",
                Icon = "PaintBrush",
                Plugin = new MockPlugin(),
                Enabled = true,
            };
            warningEntry.AddDiagnostic(PluginDiagnostic.Warning("Local skin cache folder is read-only; falling back to temporary storage", source: PluginDiagnosticSource.General));
            mockEntries.Add(warningEntry);

            // 4. Active: Multiple diagnostic records (1 Error, 1 Warning, 2 Notices)
            var mixedEntry = new PluginEntry
            {
                Id = "subdivide-nations",
                Name = "Subdivide Nations",
                Version = "1.0.0",
                Authors = new[] { new PluginAuthor("rus07tam", 33701908) },
                Tags = subdivide_nations_tags,
                Description = "Adds regional flags and subdivision ranking overlays for players.",
                Icon = "Flag",
                Plugin = new MockPlugin(),
                Enabled = true,
            };
            mixedEntry.AddDiagnostic(PluginDiagnostic.Notice("GeoIP database loaded (248 countries / regions)", source: PluginDiagnosticSource.General));
            mixedEntry.AddDiagnostic(PluginDiagnostic.Notice("High-resolution flag assets cached in memory", source: PluginDiagnosticSource.General));
            mixedEntry.AddDiagnostic(PluginDiagnostic.Warning("Plugin 'fake-supporter' version 2.0.0 is outdated (expected >= 2.1.0)", source: PluginDiagnosticSource.Dependency, target: "fake-supporter"));
            mixedEntry.AddDiagnostic(PluginDiagnostic.Error("Hook failed on ScoreV2 leaderboard render", details: "NullReferenceException: Object reference not set to an instance of an object at SubdivideNations.Patches.LeaderboardPatch.Postfix()", source: PluginDiagnosticSource.Patch, target: "LeaderboardPatch"));
            mockEntries.Add(mixedEntry);

            // 5. Error: Host Dependency Mismatch
            var hostErrorEntry = new PluginEntry
            {
                Id = "lazer-tweaks",
                Name = "Lazer Tweaks",
                Version = "3.0.0",
                Authors = new[] { new PluginAuthor("TweakDev") },
                Tags = lazer_tweaks_tags,
                Description = "Advanced tweak suite requiring cutting-edge lazer APIs.",
                Icon = "SlidersH",
                Plugin = null,
                Enabled = true,
            };
            hostErrorEntry.AddDiagnostic(PluginDiagnostic.Error("Requires newer osu!lazer (>= 2026.101.0, current: 2024.1115.0)", details: "Plugin cannot attach because host API contract has changed.", source: PluginDiagnosticSource.Dependency, target: "osu.Game"));
            mockEntries.Add(hostErrorEntry);

            // 6. Error: Bundled Dependency Missing
            var bundleErrorEntry = new PluginEntry
            {
                Id = "native-sound-engine",
                Name = "Native Sound Engine",
                Version = "1.1.0",
                Authors = new[] { new PluginAuthor("AudioGuru") },
                Tags = native_sound_engine_tags,
                Description = "Custom low-latency audio pipeline powered by native libraries.",
                Icon = "VolumeUp",
                Plugin = null,
                Enabled = true,
            };
            bundleErrorEntry.AddDiagnostic(PluginDiagnostic.Error("Bundled assembly \"libbass_fx.dll\" is missing", details: "Reinstall the plugin archive to restore bundled DLLs.", source: PluginDiagnosticSource.Bundle, target: "libbass_fx.dll"));
            mockEntries.Add(bundleErrorEntry);

            // 7. Error: Lifecycle Exception with Stack Trace
            var lifecycleErrorEntry = new PluginEntry
            {
                Id = "corrupted-plugin",
                Name = "Corrupted Hook Plugin",
                Version = "0.1.0-alpha",
                Authors = new[] { new PluginAuthor("FaultyDev") },
                Tags = corrupted_plugin_tags,
                Description = "Plugin that threw an exception during assembly patch injection.",
                Icon = "ExclamationTriangle",
                LoadError = new InvalidOperationException("Could not find target method DrawFrame() on PlayerLoader"),
                Plugin = null,
                Enabled = true,
            };
            lifecycleErrorEntry.AddDiagnostic(PluginDiagnostic.Error("Plugin Load() threw an exception", exception: new InvalidOperationException("Could not find target method DrawFrame() on PlayerLoader\n   at CorruptedPlugin.Main.OnLoad() in /src/CorruptedPlugin/Main.cs:line 42"), source: PluginDiagnosticSource.Lifecycle));
            mockEntries.Add(lifecycleErrorEntry);

            // 8. PendingEnable: Enabled but awaiting restart
            mockEntries.Add(new PluginEntry
            {
                Id = "username-visuals",
                Name = "Username Visuals",
                Version = "1.0.1",
                Authors = new[] { new PluginAuthor("rus07tam", 33701908) },
                Tags = username_visuals_tags,
                Description = "Gives supporters animated glowing and gradient username visuals.",
                Icon = "Magic",
                Plugin = null,
                Enabled = true,
                InitialEnabled = false,
            });

            // 9. PendingDisable: Loaded but disabled during this session
            mockEntries.Add(new PluginEntry
            {
                Id = "fake-supporter",
                Name = "Fake Supporter",
                Version = "2.0.0",
                Authors = new[] { new PluginAuthor("osu-cc team") },
                Tags = fake_supporter_tags,
                Description = "Unlocks client-side supporter tag features and visual perks.",
                Icon = "Heart",
                Plugin = new MockPlugin(),
                Enabled = false,
                InitialEnabled = true,
            });

            // 10. Disabled: Inactive and disabled
            mockEntries.Add(new PluginEntry
            {
                Id = "friends-leaderboard",
                Name = "Friends Leaderboard",
                Version = "1.0.0",
                Authors = new[] { new PluginAuthor("rus07tam", 33701908) },
                Tags = friends_leaderboard_tags,
                Description = "Displays friend rankings directly on song select without opening profile.",
                Icon = "UserFriends",
                Plugin = null,
                Enabled = false,
                InitialEnabled = false,
            });

            // 11. PendingDelete: Marked for deletion on next launch
            mockEntries.Add(new PluginEntry
            {
                Id = "example-plugin",
                Name = "Example Plugin",
                Version = "0.9.5",
                Authors = new[] { new PluginAuthor("ExampleDev") },
                Tags = example_plugin_tags,
                Description = "Demonstration template plugin marked for deletion on next launch.",
                Icon = "PuzzlePiece",
                Plugin = new MockPlugin(),
                Enabled = false,
                InitialEnabled = false,
                PendingDelete = true,
            });

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
                    if (PluginNameLink.ShowDetailsEntryHandler != null)
                    {
                        PluginNameLink.ShowDetailsEntryHandler(c.Entry);
                    }
                    else
                    {
                        host.Notify($"Mock card clicked: '{c.Entry.Name}' (Status: {c.Entry.Status})", NotificationKind.Info);
                    }
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
