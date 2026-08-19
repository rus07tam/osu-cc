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

namespace CustomUserGroups
{
    /// <summary>
    /// Settings UI for per-user group overrides: a form to add or update an override for a user id
    /// (a user id plus the custom group to force) and a list of the current overrides with
    /// edit/remove buttons. Overrides are persisted through the plugin's <c>user_overrides</c>
    /// setting.
    /// </summary>
    public partial class CustomUserGroupsUserOverridesSection : CompositeDrawable
    {
        private readonly CustomUserGroupsApi api;

        private readonly IOsuCcPluginHost host;

        private readonly FormNumberBox userIdBox;
        private readonly GroupDropdown groupDropdown;
        private readonly FillFlowContainer listFlow;
        private readonly OsuSpriteText overridesHeader;
        private readonly OsuSpriteText listHeader;

        // Real usernames fetched by id for the row previews (per-user overrides are keyed by id).
        private readonly Dictionary<int, string> resolvedNames = new();

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private UserLookupCache userLookupCache { get; set; } = null!;

        public CustomUserGroupsUserOverridesSection(CustomUserGroupsApi api, IOsuCcPluginHost host)
        {
            this.api = api;
            this.host = host;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            userIdBox = new FormNumberBox
            {
                Caption = CustomUserGroupsStrings.UserOverrideIdCaption,
                PlaceholderText = CustomUserGroupsStrings.UserOverrideIdPlaceholder,
            };

            groupDropdown = new GroupDropdown(api)
            {
                Caption = CustomUserGroupsStrings.UserOverrideGroupCaption,
                HintText = CustomUserGroupsStrings.UserOverrideGroupHint,
            };

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(8),
                Children = new Drawable[]
                {
                    buildPaddedHeader(CustomUserGroupsStrings.UserOverridesSectionCaption, out overridesHeader),
                    new SettingsItemV2(userIdBox),
                    new SettingsItemV2(groupDropdown),
                    new SettingsButtonV2
                    {
                        Text = CustomUserGroupsStrings.UserOverrideApplyButtonText,
                        Action = applyOverride,
                    },
                    buildPaddedHeader(CustomUserGroupsStrings.UserOverridesListCaption, out listHeader),
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

        private void onApiChanged()
        {
            Scheduler.AddOnce(rebuildList);
            Scheduler.AddOnce(groupDropdown.RefreshItems);
        }

        private void applyOverride()
        {
            // Empty text boxes report a null bindable value until the user types, so trim null-safe.
            string rawUserId = userIdBox.Current.Value?.Trim() ?? string.Empty;

            if (!int.TryParse(rawUserId, out int userId) || userId <= 0)
                return;

            int groupId = groupDropdown.Current.Value;
            if (groupId <= 0 || api.Groups.All(g => g.Id != groupId))
                return;

            api.SetPersistedOverride(new UserGroupOverride
            {
                UserId = userId,
                GroupId = groupId,
            });

            clearForm();
        }

        private void clearForm()
        {
            userIdBox.Current.Value = string.Empty;
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
                        Text = CustomUserGroupsStrings.NoUserOverrides,
                        Alpha = 0.6f,
                    },
                });
                return;
            }

            foreach (var userOverride in overrides)
                listFlow.Add(buildOverrideRow(userOverride));
        }

        private Container buildOverrideRow(UserGroupOverride userOverride)
        {
            // Real username by id when known; the id doubles as a placeholder until it resolves.
            if (!resolvedNames.TryGetValue(userOverride.UserId, out string? displayName))
            {
                displayName = userOverride.UserId.ToString(CultureInfo.InvariantCulture);
                requestUserName(userOverride.UserId);
            }

            var group = api.Groups.FirstOrDefault(g => g.Id == userOverride.GroupId);

            string badgeText = group?.ShortName ?? $"#{userOverride.GroupId}";

            // A rounded card in the FormControlBackground tone, with the [id] prefix, username and
            // group badge on the left and edit/delete icon buttons on the right — matching the
            // PluginCard list in the host.
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
                            Spacing = new Vector2(8),
                            Padding = new MarginPadding { Left = 14, Right = 90 },
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = $"[{userOverride.UserId}]",
                                    Alpha = 0.5f,
                                    Font = OsuFont.GetFont(size: 15),
                                },
                                new OsuSpriteText
                                {
                                    Text = displayName,
                                    Font = OsuFont.GetFont(size: 15),
                                },
                                new OsuSpriteText
                                {
                                    Text = badgeText,
                                    Font = OsuFont.GetFont(size: 13, weight: FontWeight.SemiBold),
                                    Colour = group?.Colour is { Length: > 0 } hex
                                        ? Color4Extensions.FromHex(hex)
                                        : Color4Extensions.FromHex("7a7a7a"),
                                },
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
                                    TooltipText = CustomUserGroupsStrings.UserOverrideEditTooltip,
                                    IconColour = colourProvider.Foreground1,
                                    Action = () => editOverride(userOverride),
                                },
                                new IconButton
                                {
                                    Icon = FontAwesome.Solid.Times,
                                    TooltipText = CustomUserGroupsStrings.UserOverrideDeleteTooltip,
                                    IconColour = OsuCcColours.Error,
                                    Action = () => host.Confirm(
                                        CustomUserGroupsStrings.UserOverrideDeleteTitle,
                                        CustomUserGroupsStrings.UserOverrideDeleteBody(userOverride.UserId),
                                        () => api.RemovePersistedOverride(userOverride.UserId, userOverride.GroupId)),
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
        private void editOverride(UserGroupOverride userOverride)
        {
            userIdBox.Current.Value = userOverride.UserId.ToString(CultureInfo.InvariantCulture);
            groupDropdown.Current.Value = userOverride.GroupId;

            api.RemovePersistedOverride(userOverride.UserId, userOverride.GroupId);
        }
    }

    /// <summary>
    /// Group picker for a per-user override. Items track the plugin's current group library, which
    /// the owning section refreshes via <see cref="RefreshItems"/> whenever it changes.
    /// </summary>
    public partial class GroupDropdown : FormDropdown<int>
    {
        private readonly CustomUserGroupsApi api;

        public GroupDropdown(CustomUserGroupsApi api)
        {
            this.api = api;
            RefreshItems();
        }

        public void RefreshItems()
        {
            var groups = api.Groups;

            Items = groups.Select(g => g.Id).ToArray();

            if (Current.Value <= 0 || groups.All(g => g.Id != Current.Value))
                Current.Value = groups.Count > 0 ? groups[0].Id : 0;
        }

        protected override LocalisableString GenerateItemText(int item)
        {
            var group = api.Groups.FirstOrDefault(g => g.Id == item);
            return group == null ? $"#{item}" : $"{group.ShortName} — {group.Name}";
        }
    }
}
