using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
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
    /// Settings UI for the custom group library: a form to add or edit a group (id, name, short
    /// name, identifier, colour, playmodes, probationary) and a list of the current groups with
    /// edit/remove buttons. Groups are persisted as a JSON list through the plugin's
    /// <c>groups</c> setting.
    /// </summary>
    public partial class CustomUserGroupsGroupEditorSection : CompositeDrawable
    {
        private readonly CustomUserGroupsApi api;

        private readonly IOsuCcPluginHost host;

        private readonly FormNumberBox idBox;
        private readonly FormTextBox nameBox;
        private readonly FormTextBox shortNameBox;
        private readonly FormTextBox identifierBox;
        private readonly OsuCcColourPalette colourPalette;
        private readonly FormTextBox playmodesBox;
        private readonly FormCheckBox probationaryBox;
        private readonly SettingsButtonV2 applyButton;
        private readonly FillFlowContainer listFlow;
        private readonly OsuSpriteText editorHeader;
        private readonly OsuSpriteText listHeader;

        // A group id being edited; null means "add a new group".
        private int? editingId;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        public CustomUserGroupsGroupEditorSection(CustomUserGroupsApi api, IOsuCcPluginHost host)
        {
            this.api = api;
            this.host = host;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            idBox = new FormNumberBox
            {
                Caption = CustomUserGroupsStrings.GroupIdCaption,
                PlaceholderText = CustomUserGroupsStrings.GroupIdPlaceholder,
            };

            nameBox = new FormTextBox
            {
                Caption = CustomUserGroupsStrings.GroupNameCaption,
                PlaceholderText = CustomUserGroupsStrings.GroupNamePlaceholder,
            };

            shortNameBox = new FormTextBox
            {
                Caption = CustomUserGroupsStrings.GroupShortNameCaption,
                PlaceholderText = CustomUserGroupsStrings.GroupShortNamePlaceholder,
            };

            identifierBox = new FormTextBox
            {
                Caption = CustomUserGroupsStrings.GroupIdentifierCaption,
                PlaceholderText = CustomUserGroupsStrings.GroupIdentifierPlaceholder,
            };

            colourPalette = new OsuCcColourPalette
            {
                Caption = CustomUserGroupsStrings.GroupColourCaption,
                HintText = CustomUserGroupsStrings.GroupColourHint,
                RelativeSizeAxes = Axes.X,
            };

            playmodesBox = new FormTextBox
            {
                Caption = CustomUserGroupsStrings.GroupPlaymodesCaption,
                PlaceholderText = CustomUserGroupsStrings.GroupPlaymodesPlaceholder,
            };

            probationaryBox = new FormCheckBox
            {
                Caption = CustomUserGroupsStrings.GroupProbationaryCaption,
                HintText = CustomUserGroupsStrings.GroupProbationaryHint,
            };

            applyButton = new SettingsButtonV2
            {
                Text = CustomUserGroupsStrings.GroupApplyButtonText,
                Action = applyGroup,
            };

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(8),
                Children = new Drawable[]
                {
                    buildPaddedHeader(CustomUserGroupsStrings.GroupEditorSectionCaption, out editorHeader),
                    new SettingsItemV2(idBox),
                    new SettingsItemV2(nameBox),
                    new SettingsItemV2(shortNameBox),
                    new SettingsItemV2(identifierBox),
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = SettingsPanel.CONTENT_PADDING,
                        Child = colourPalette,
                    },
                    new SettingsItemV2(playmodesBox),
                    new SettingsItemV2(probationaryBox),
                    applyButton,
                    buildPaddedHeader(CustomUserGroupsStrings.GroupListCaption, out listHeader),
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
            editorHeader.Colour = colourProvider.Content2;
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

        private void applyGroup()
        {
            // Empty text boxes report a null bindable value until the user types, so trim null-safe.
            string rawId = idBox.Current.Value?.Trim() ?? string.Empty;

            if (!int.TryParse(rawId, out int groupId) || groupId <= 0)
                return;

            var groups = api.PersistedGroups.ToList();

            if (editingId != null)
            {
                groups.RemoveAll(g => g.Id == editingId.Value);
            }
            else if (groups.Any(g => g.Id == groupId))
            {
                // Duplicate id; leave the list untouched so the user can pick another.
                return;
            }

            groups.Add(new CustomUserGroup
            {
                Id = groupId,
                Identifier = emptyToNull(identifierBox.Current.Value),
                Name = nameBox.Current.Value?.Trim() ?? string.Empty,
                ShortName = shortNameBox.Current.Value?.Trim() ?? string.Empty,
                Colour = colourPalette.Colours.Count > 0 ? colourPalette.Colours[0].ToHex() : null,
                IsProbationary = probationaryBox.Current.Value,
                Playmodes = parsePlaymodes(playmodesBox.Current.Value),
            });

            api.SetGroups(groups);
            clearForm();
        }

        private static string[]? parsePlaymodes(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var modes = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                             .Where(m => m.Length > 0)
                             .ToArray();

            return modes.Length > 0 ? modes : null;
        }

        private static string? emptyToNull(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private void clearForm()
        {
            editingId = null;
            idBox.Current.Value = string.Empty;
            nameBox.Current.Value = string.Empty;
            shortNameBox.Current.Value = string.Empty;
            identifierBox.Current.Value = string.Empty;
            colourPalette.Colours.Clear();
            playmodesBox.Current.Value = string.Empty;
            probationaryBox.Current.Value = false;
            applyButton.Text = CustomUserGroupsStrings.GroupApplyButtonText;
        }

        private void rebuildList()
        {
            listFlow.Clear();

            var groups = api.PersistedGroups;

            if (groups.Count == 0)
            {
                listFlow.Add(new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = SettingsPanel.CONTENT_PADDING,
                    Child = new OsuSpriteText
                    {
                        Text = CustomUserGroupsStrings.NoGroups,
                        Alpha = 0.6f,
                    },
                });
                return;
            }

            foreach (var group in groups)
                listFlow.Add(buildGroupRow(group));
        }

        private Container buildGroupRow(CustomUserGroup group)
        {
            string badgeText = string.IsNullOrEmpty(group.ShortName) ? $"#{group.Id}" : group.ShortName;

            // A rounded card in the FormControlBackground tone, with the [id] prefix, short name
            // and full name on the left and edit/delete icon buttons on the right.
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
                                    Text = $"[{group.Id}]",
                                    Alpha = 0.5f,
                                    Font = OsuFont.GetFont(size: 15),
                                },
                                new OsuSpriteText
                                {
                                    Text = badgeText,
                                    Font = OsuFont.GetFont(size: 15, weight: FontWeight.Bold),
                                    Colour = group.Colour is { Length: > 0 } hex
                                        ? Color4Extensions.FromHex(hex)
                                        : Color4Extensions.FromHex("ffffff"),
                                },
                                new OsuSpriteText
                                {
                                    Text = string.IsNullOrEmpty(group.Name) ? string.Empty : $"({group.Name})",
                                    Font = OsuFont.GetFont(size: 13),
                                    Alpha = 0.8f,
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
                                    TooltipText = CustomUserGroupsStrings.GroupEditTooltip,
                                    IconColour = colourProvider.Foreground1,
                                    Action = () => editGroup(group),
                                },
                                new IconButton
                                {
                                    Icon = FontAwesome.Solid.Times,
                                    TooltipText = CustomUserGroupsStrings.GroupDeleteTooltip,
                                    IconColour = OsuCcColours.Error,
                                    Action = () => host.Confirm(
                                        CustomUserGroupsStrings.GroupDeleteTitle,
                                        CustomUserGroupsStrings.GroupDeleteBody(group),
                                        () => deleteGroup(group)),
                                },
                            },
                        },
                    },
                },
            };
        }

        /// <summary>Loads a group into the form so Apply re-creates it, then removes the original item.</summary>
        private void editGroup(CustomUserGroup group)
        {
            editingId = group.Id;
            idBox.Current.Value = group.Id.ToString(CultureInfo.InvariantCulture);
            nameBox.Current.Value = group.Name ?? string.Empty;
            shortNameBox.Current.Value = group.ShortName ?? string.Empty;
            identifierBox.Current.Value = group.Identifier ?? string.Empty;

            colourPalette.Colours.Clear();
            if (group.Colour is { Length: > 0 } hex)
                colourPalette.Colours.Add(Color4Extensions.FromHex(hex));

            playmodesBox.Current.Value = group.Playmodes == null ? string.Empty : string.Join(",", group.Playmodes);
            probationaryBox.Current.Value = group.IsProbationary;
            applyButton.Text = CustomUserGroupsStrings.GroupEditButtonText;

            deleteGroup(group);
        }

        private void deleteGroup(CustomUserGroup group) => api.RemoveGroup(group.Id);
    }
}
