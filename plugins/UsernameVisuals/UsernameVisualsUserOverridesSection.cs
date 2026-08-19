using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osucc.Core;
using osucc.Plugin;
using osuTK;
using System.Globalization;
using System.Linq;

namespace UsernameVisuals
{
    /// <summary>
    /// Settings UI for per-user colour/display overrides: a form to add or update an override for
    /// a user id (gradient palette, display name, hide) plus a list of the current overrides with
    /// remove buttons. Overrides are persisted through the plugin's <c>user_overrides</c> setting.
    /// </summary>
    public partial class UsernameVisualsUserOverridesSection : CompositeDrawable
    {
        private readonly UsernameVisualsApi api;

        private readonly IOsuCcPluginHost host;

        private readonly FormNumberBox userIdBox;
        private readonly OsuCcColourPalette palette;
        private readonly FormTextBox nameBox;
        private readonly FormCheckBox hideBox;
        private readonly FillFlowContainer listFlow;
        private readonly OsuSpriteText overridesHeader;
        private readonly OsuSpriteText listHeader;

        // Real usernames fetched by id for the row previews (per-user overrides are keyed by id).
        private readonly Dictionary<int, string> resolvedNames = new();

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private UserLookupCache userLookupCache { get; set; } = null!;

        public UsernameVisualsUserOverridesSection(UsernameVisualsApi api, IOsuCcPluginHost host)
        {
            this.api = api;
            this.host = host;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            userIdBox = new FormNumberBox
            {
                Caption = UsernameVisualsStrings.UserOverrideIdCaption,
                PlaceholderText = UsernameVisualsStrings.UserOverrideIdPlaceholder,
            };

            palette = new OsuCcColourPalette
            {
                Caption = UsernameVisualsStrings.UserOverridePaletteCaption,
                HintText = UsernameVisualsStrings.UserOverridePaletteHint,
                RelativeSizeAxes = Axes.X,
            };

            nameBox = new FormTextBox
            {
                Caption = UsernameVisualsStrings.UserOverrideNameCaption,
                PlaceholderText = UsernameVisualsStrings.UserOverrideNamePlaceholder,
            };

            hideBox = new FormCheckBox
            {
                Caption = UsernameVisualsStrings.UserOverrideHideCaption,
                HintText = UsernameVisualsStrings.UserOverrideHideHint,
            };

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(8),
                Children = new Drawable[]
                {
                    buildPaddedHeader(UsernameVisualsStrings.UserOverridesSectionCaption, out overridesHeader),
                    new SettingsItemV2(userIdBox),
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = SettingsPanel.CONTENT_PADDING,
                        Child = palette,
                    },
                    new SettingsItemV2(nameBox),
                    new SettingsItemV2(hideBox),
                    new SettingsButtonV2
                    {
                        Text = UsernameVisualsStrings.UserOverrideApplyButtonText,
                        Action = applyOverride,
                    },
                    buildPaddedHeader(UsernameVisualsStrings.UserOverridesListCaption, out listHeader),
                    listFlow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(4),
                    },
                },
            };
        }

        private static Container buildPaddedHeader(LocalisableString text, out OsuSpriteText header) => new()
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Padding = SettingsPanel.CONTENT_PADDING,
            Child = header = new OsuSpriteText
            {
                Text = text,
                Font = OsuFont.GetFont(size: 15, weight: FontWeight.Bold),
            },
        };

        protected override void LoadComplete()
        {
            base.LoadComplete();
            overridesHeader.Colour = colourProvider.Content2;
            listHeader.Colour = colourProvider.Content2;
            api.Changed += onApiChanged;
            rebuildList();
        }

        protected override void Dispose(bool isDisposing)
        {
            api.Changed -= onApiChanged;
            base.Dispose(isDisposing);
        }

        private void onApiChanged() => Scheduler.AddOnce(rebuildList);

        private void applyOverride()
        {
            // Empty text boxes report a null bindable value until the user types, so trim null-safe.
            string rawUserId = userIdBox.Current.Value?.Trim() ?? string.Empty;

            if (!int.TryParse(rawUserId, out int userId) || userId <= 0)
                return;

            string paletteValue = string.Join(",", palette.Colours.Select(c => c.ToHex()));
            string nameValue = (nameBox.Current.Value ?? string.Empty).Trim();

            if (paletteValue.Length == 0 && nameValue.Length == 0 && !hideBox.Current.Value)
                return;

            api.SetPersistedOverride(new UsernameUserOverride
            {
                UserId = userId,
                Palette = paletteValue,
                Name = nameValue,
                Hide = hideBox.Current.Value,
            });

            clearForm();
        }

        private void clearForm()
        {
            userIdBox.Current.Value = string.Empty;
            nameBox.Current.Value = string.Empty;
            hideBox.Current.Value = false;
            palette.Colours.Clear();
        }

        private void rebuildList()
        {
            listFlow.Clear();

            var overrides = api.PersistedOverrides;

            if (overrides.Count == 0)
            {
                listFlow.Add(new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = SettingsPanel.CONTENT_PADDING,
                    Child = new OsuSpriteText
                    {
                        Text = UsernameVisualsStrings.NoUserOverrides,
                        Alpha = 0.6f,
                    },
                });
                return;
            }

            foreach (var userOverride in overrides)
                listFlow.Add(buildOverrideRow(userOverride));
        }

        private Container buildOverrideRow(UsernameUserOverride userOverride)
        {
            // Real username by id when known; the id doubles as a placeholder until it resolves.
            if (!resolvedNames.TryGetValue(userOverride.UserId, out string? displayName))
            {
                displayName = userOverride.UserId.ToString(CultureInfo.InvariantCulture);
                requestUserName(userOverride.UserId);
            }

            // A live preview reusing the plugin's own text: it shows the current gradient, the
            // replace override (or the real username when none) and the canonical hide block.
            var preview = new UsernameVisualsText
            {
                Text = displayName,
                User = new APIUser { Id = userOverride.UserId, Username = displayName },
                Font = OsuFont.GetFont(size: 15),
            };

            // A rounded card in the FormControlBackground tone, with the [id] prefix and preview
            // on the left and edit/delete icon buttons on the right — matching the PluginCard
            // list in the host.
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = SettingsPanel.CONTENT_PADDING,
                Child = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Masking = true,
                    CornerRadius = 5,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background4.Darken(0.1f),
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(6),
                            Padding = new MarginPadding { Left = 14, Right = 90 },
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = $"[{userOverride.UserId}]",
                                    Alpha = 0.5f,
                                    Font = OsuFont.GetFont(size: 15),
                                },
                                preview,
                            },
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(4),
                            Padding = new MarginPadding { Right = 8 },
                            Children = new Drawable[]
                            {
                                new IconButton
                                {
                                    Icon = FontAwesome.Solid.PencilAlt,
                                    TooltipText = UsernameVisualsStrings.UserOverrideEditTooltip,
                                    IconColour = colourProvider.Foreground1,
                                    Action = () => editOverride(userOverride),
                                },
                                new IconButton
                                {
                                    Icon = FontAwesome.Solid.Times,
                                    TooltipText = UsernameVisualsStrings.UserOverrideDeleteTooltip,
                                    IconColour = OsuCcColours.Error,
                                    Action = () => host.Confirm(
                                        UsernameVisualsStrings.UserOverrideDeleteTitle,
                                        UsernameVisualsStrings.UserOverrideDeleteBody(userOverride.UserId),
                                        () => api.RemovePersistedOverride(userOverride.UserId)),
                                },
                            },
                        },
                    },
                },
            };
        }

        /// <summary>
        /// Fetches the real username for an id through the game's cached user lookup and swaps it
        /// into the preview once available. Best-effort: failures keep the id as the placeholder.
        /// </summary>
        private void requestUserName(int userId)
        {
            try
            {
                Task<APIUser?> lookup = userLookupCache.GetUserAsync(userId);

                if (lookup.IsCompletedSuccessfully)
                {
                    if (lookup.Result is { Username: { Length: > 0 } } user)
                        resolvedNames[userId] = user.Username;

                    return;
                }

                lookup.ContinueWith(task =>
                {
                    if (IsDisposed)
                        return;

                    if (task.IsCompletedSuccessfully && task.Result is { Username: { Length: > 0 } } user)
                    {
                        resolvedNames[userId] = user.Username;
                        Scheduler.AddOnce(rebuildList);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch
            {
                // lookup unavailable; keep showing the id
            }
        }

        /// <summary>Loads an override into the form so Apply re-creates it, then removes the original item.</summary>
        private void editOverride(UsernameUserOverride userOverride)
        {
            userIdBox.Current.Value = userOverride.UserId.ToString(CultureInfo.InvariantCulture);
            nameBox.Current.Value = userOverride.Name ?? string.Empty;
            hideBox.Current.Value = userOverride.Hide;

            palette.Colours.Clear();

            foreach (var colour in SettingsSubsectionExtensions.ParsePalette(userOverride.Palette))
                palette.Colours.Add(colour);

            api.RemovePersistedOverride(userOverride.UserId);
        }
    }
}
