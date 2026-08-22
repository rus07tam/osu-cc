using osu.Framework.Allocation;
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
using osu.Game.Overlays;
using osucc.Client;
using osucc.Core;
using osucc.Localisation;
using osucc.Plugin;
using osucc.UI.Overlays;
using osuTK;
using osuTK.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace osucc.UI.Plugins
{
    /// <summary>
    /// Full-screen diagnostics overlay displaying all error, warning, and notice records registered for a plugin.
    /// </summary>
    public partial class PluginDiagnosticsOverlay : OsuCcWaveOverlay
    {
        [Resolved]
        private osu.Framework.Platform.Clipboard? clipboard { get; set; }

        private readonly FillFlowContainer content;
        private PluginEntry? displayedEntry;

        public PluginDiagnosticsOverlay()
            : base(OverlayColourScheme.Red)
        {
            content = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Padding = new MarginPadding
                {
                    Vertical = 20,
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Header.TitleText = PluginsOverlayStrings.DetailsDiagnosticsTitle;
            Header.HeaderIcon = FontAwesome.Solid.Bug;
            MainAreaContent.Add(content);
        }

        /// <summary>Populates and opens the diagnostics overlay for the given plugin entry.</summary>
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

            rebuild(entry);
        }

        private void updateDisplayedEntryUi()
        {
            if (displayedEntry != null && State.Value == Visibility.Visible)
                rebuild(displayedEntry);
        }

        private void rebuild(PluginEntry entry)
        {
            content.Clear();

            LocalisableString pluginName = PluginCardLayout.LocalisedName(entry);
            Header.TitleText = PluginsOverlayStrings.DetailsDiagnosticsTitle;
            Header.DescriptionText = LocalisableString.Format("{0} \u2022 v{1}", pluginName, entry.Version);

            var pillsFlow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(6, 0),
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
            };

            if (entry.ErrorCount > 0)
                pillsFlow.Add(new DiagnosticPill(PluginDiagnosticLevel.Error, entry.ErrorCount));
            if (entry.WarningCount > 0)
                pillsFlow.Add(new DiagnosticPill(PluginDiagnosticLevel.Warning, entry.WarningCount));
            if (entry.NoticeCount > 0)
                pillsFlow.Add(new DiagnosticPill(PluginDiagnosticLevel.Notice, entry.NoticeCount));

            // Summary Header Row: Title and pills on the same horizontal line (CentreLeft aligned)
            var summaryRow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(10, 0),
                Padding = new MarginPadding { Bottom = 8 },
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = pluginName,
                        Font = OsuFont.Torus.With(size: 20, weight: FontWeight.Bold),
                        Colour = ColourProvider.Content1,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    },
                    pillsFlow,
                },
            };

            content.Add(summaryRow);

            var diagnostics = entry.Diagnostics;

            if (diagnostics.Count == 0)
            {
                content.Add(new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 120,
                    CornerRadius = 8,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = ColourProvider.Background4,
                        },
                        new FillFlowContainer
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(10, 0),
                            Children = new Drawable[]
                            {
                                new SpriteIcon
                                {
                                    Icon = FontAwesome.Solid.CheckCircle,
                                    Size = new Vector2(20),
                                    Colour = OsuCcColours.Success,
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                },
                                new OsuSpriteText
                                {
                                    Text = PluginsOverlayStrings.DiagnosticsEmpty,
                                    Font = OsuFont.Torus.With(size: 16, weight: FontWeight.SemiBold),
                                    Colour = ColourProvider.Content2,
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                },
                            },
                        },
                    },
                });
            }
            else
            {
                foreach (var diag in diagnostics)
                {
                    content.Add(createDiagnosticCard(diag));
                }
            }
        }

        private Container createDiagnosticCard(PluginDiagnostic diag)
        {
            var (icon, accentColour) = diag.Level switch
            {
                PluginDiagnosticLevel.Error => (FontAwesome.Solid.TimesCircle, OsuCcColours.Error),
                PluginDiagnosticLevel.Warning => (FontAwesome.Solid.ExclamationTriangle, OsuCcColours.Warning),
                _ => (FontAwesome.Solid.InfoCircle, OsuCcColours.Info),
            };

            LocalisableString sourceName = diag.Source switch
            {
                PluginDiagnosticSource.Lifecycle => PluginsOverlayStrings.DiagnosticsSourceLifecycle,
                PluginDiagnosticSource.Patch => PluginsOverlayStrings.DiagnosticsSourcePatch,
                PluginDiagnosticSource.Dependency => PluginsOverlayStrings.DiagnosticsSourceDependency,
                PluginDiagnosticSource.Bundle => PluginsOverlayStrings.DiagnosticsSourceBundle,
                _ => PluginsOverlayStrings.DiagnosticsSourceGeneral,
            };

            var topRow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(8, 0),
                Children = new Drawable[]
                {
                    new SpriteIcon
                    {
                        Icon = icon,
                        Size = new Vector2(16),
                        Colour = accentColour,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    },
                    new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        CornerRadius = 4,
                        Masking = true,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = accentColour.Opacity(0.2f),
                            },
                            new OsuSpriteText
                            {
                                Text = sourceName,
                                Font = OsuFont.Torus.With(size: 11, weight: FontWeight.Bold),
                                Colour = accentColour,
                                Padding = new MarginPadding { Horizontal = 6, Vertical = 2 },
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                            },
                        },
                    },
                },
            };

            if (!string.IsNullOrEmpty(diag.Target))
            {
                topRow.Add(new OsuSpriteText
                {
                    Text = $"[{diag.Target}]",
                    Font = OsuFont.Torus.With(size: 13, weight: FontWeight.Medium),
                    Colour = ColourProvider.Content2,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                });
            }

            topRow.Add(new OsuSpriteText
            {
                Text = diag.Timestamp.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                Font = OsuFont.Default.With(size: 11),
                Colour = ColourProvider.Content2.Opacity(0.7f),
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
            });

            var cardFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 8),
                Padding = new MarginPadding(12),
                Children = new Drawable[]
                {
                    topRow,
                    new OsuSpriteText
                    {
                        RelativeSizeAxes = Axes.X,
                        Text = diag.Message,
                        Font = OsuFont.Torus.With(size: 14, weight: FontWeight.Regular),
                        Colour = ColourProvider.Content1,
                    },
                },
            };

            string? detailText = diag.Details ?? diag.Exception?.ToString();
            if (!string.IsNullOrWhiteSpace(detailText))
            {
                cardFlow.Add(new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    CornerRadius = 6,
                    Masking = true,
                    Margin = new MarginPadding { Top = 4 },
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = ColourProvider.Background6,
                        },
                        new OsuTextFlowContainer(cp => cp.Font = OsuFont.Default.With(size: 14, fixedWidth: true))
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Text = detailText,
                            Colour = ColourProvider.Content2,
                            Padding = new MarginPadding(10),
                        },
                    },
                });
            }

            string copyContent = $"[{diag.Level}] {diag.Source}\n{diag.Message}";
            if (!string.IsNullOrEmpty(detailText))
                copyContent += $"\n{detailText}";

            return new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                CornerRadius = 8,
                Masking = true,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourProvider.Background4,
                    },
                    cardFlow,
                    new IconButton
                    {
                        Icon = FontAwesome.Solid.Copy,
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Margin = new MarginPadding(8),
                        Size = new Vector2(30),
                        TooltipText = "Copy diagnostic details",
                        Action = () =>
                        {
                            clipboard?.SetText(copyContent);
                            ClientNotifications.Success("Copied diagnostic to clipboard");
                        }
                    }
                },
            };
        }

        protected override void Dispose(bool isDisposing)
        {
            if (displayedEntry != null)
                displayedEntry.StateChanged -= updateDisplayedEntryUi;

            base.Dispose(isDisposing);
        }
    }
}
