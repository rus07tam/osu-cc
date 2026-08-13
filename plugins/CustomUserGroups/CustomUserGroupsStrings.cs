using osu.Framework.Localisation;
using osucc.Localisation;

namespace CustomUserGroups
{
    public static class CustomUserGroupsStrings
    {
        private const string prefix = "custom-user-groups";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString Name => OsuCcLocalisation.Get($"{prefix}:name", "Custom User Groups");

        public static LocalisableString Description => OsuCcLocalisation.Get($"{prefix}:description", "Shows custom user groups (colour, short name, name, playmodes) on profiles and user cards, appended to real osu! groups, with per-user overrides and a public rule API.");

        public static LocalisableString EnabledCaption => OsuCcLocalisation.Get(getKey(nameof(EnabledCaption)), "Custom user groups");

        public static LocalisableString EnabledHint => OsuCcLocalisation.Get(getKey(nameof(EnabledHint)), "Shows custom user groups (badges and username colour) appended to the real ones, everywhere groups are displayed. Local cosmetic only: nothing is sent to the servers.");

        public static LocalisableString ApplyColourCaption => OsuCcLocalisation.Get(getKey(nameof(ApplyColourCaption)), "Apply badge colour to username");

        public static LocalisableString ApplyColourHint => OsuCcLocalisation.Get(getKey(nameof(ApplyColourHint)), "Uses the custom group's colour for the username everywhere.");

        public static LocalisableString ApplyColourHintMissing => OsuCcLocalisation.Get(getKey(nameof(ApplyColourHintMissing)), "Uses the custom group's colour for the username everywhere (Requires Username Visuals plugin).");

        public static LocalisableString GroupEditorSectionCaption => OsuCcLocalisation.Get(getKey(nameof(GroupEditorSectionCaption)), "Custom groups");

        public static LocalisableString GroupListCaption => OsuCcLocalisation.Get(getKey(nameof(GroupListCaption)), "Current groups");

        public static LocalisableString GroupEditorAddButtonText => OsuCcLocalisation.Get(getKey(nameof(GroupEditorAddButtonText)), "Add group");

        public static LocalisableString GroupIdCaption => OsuCcLocalisation.Get(getKey(nameof(GroupIdCaption)), "ID");

        public static LocalisableString GroupIdPlaceholder => OsuCcLocalisation.Get(getKey(nameof(GroupIdPlaceholder)), "Unique number");

        public static LocalisableString GroupNameCaption => OsuCcLocalisation.Get(getKey(nameof(GroupNameCaption)), "Name");

        public static LocalisableString GroupNamePlaceholder => OsuCcLocalisation.Get(getKey(nameof(GroupNamePlaceholder)), "Full group name");

        public static LocalisableString GroupShortNameCaption => OsuCcLocalisation.Get(getKey(nameof(GroupShortNameCaption)), "Short name");

        public static LocalisableString GroupShortNamePlaceholder => OsuCcLocalisation.Get(getKey(nameof(GroupShortNamePlaceholder)), "Badge text (e.g. GMT)");

        public static LocalisableString GroupIdentifierCaption => OsuCcLocalisation.Get(getKey(nameof(GroupIdentifierCaption)), "Identifier");

        public static LocalisableString GroupIdentifierPlaceholder => OsuCcLocalisation.Get(getKey(nameof(GroupIdentifierPlaceholder)), "Optional, defaults to short name");

        public static LocalisableString GroupColourCaption => OsuCcLocalisation.Get(getKey(nameof(GroupColourCaption)), "Colour");

        public static LocalisableString GroupColourHint => OsuCcLocalisation.Get(getKey(nameof(GroupColourHint)), "Badge and username colour. The palette takes a single colour; leave it empty for no colour.");

        public static LocalisableString GroupProbationaryCaption => OsuCcLocalisation.Get(getKey(nameof(GroupProbationaryCaption)), "Probationary");

        public static LocalisableString GroupProbationaryHint => OsuCcLocalisation.Get(getKey(nameof(GroupProbationaryHint)), "Shows the badge at reduced opacity, like real probationary groups.");

        public static LocalisableString GroupPlaymodesCaption => OsuCcLocalisation.Get(getKey(nameof(GroupPlaymodesCaption)), "Playmodes");

        public static LocalisableString GroupPlaymodesPlaceholder => OsuCcLocalisation.Get(getKey(nameof(GroupPlaymodesPlaceholder)), "Comma-separated (osu, taiko, …)");

        public static LocalisableString GroupApplyButtonText => OsuCcLocalisation.Get(getKey(nameof(GroupApplyButtonText)), "Apply");

        public static LocalisableString GroupEditButtonText => OsuCcLocalisation.Get(getKey(nameof(GroupEditButtonText)), "Save");

        public static LocalisableString GroupEditTooltip => OsuCcLocalisation.Get(getKey(nameof(GroupEditTooltip)), "Edit");

        public static LocalisableString GroupDeleteTooltip => OsuCcLocalisation.Get(getKey(nameof(GroupDeleteTooltip)), "Delete");

        public static LocalisableString NoGroups => OsuCcLocalisation.Get(getKey(nameof(NoGroups)), "No custom groups yet. Add one above.");

        public static LocalisableString UserOverridesSectionCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverridesSectionCaption)), "Per-user overrides");

        public static LocalisableString UserOverrideIdCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverrideIdCaption)), "User ID");

        public static LocalisableString UserOverrideIdPlaceholder => OsuCcLocalisation.Get(getKey(nameof(UserOverrideIdPlaceholder)), "osu! user ID");

        public static LocalisableString UserOverrideGroupCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverrideGroupCaption)), "Group");

        public static LocalisableString UserOverrideGroupHint => OsuCcLocalisation.Get(getKey(nameof(UserOverrideGroupHint)), "Force this custom group onto the user's profile everywhere (on top of their real groups).");

        public static LocalisableString UserOverrideApplyButtonText => OsuCcLocalisation.Get(getKey(nameof(UserOverrideApplyButtonText)), "Apply");

        public static LocalisableString UserOverridesListCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverridesListCaption)), "Current overrides");

        public static LocalisableString NoUserOverrides => OsuCcLocalisation.Get(getKey(nameof(NoUserOverrides)), "No per-user overrides yet.");

        public static LocalisableString UserOverrideEditTooltip => OsuCcLocalisation.Get(getKey(nameof(UserOverrideEditTooltip)), "Edit");

        public static LocalisableString UserOverrideDeleteTooltip => OsuCcLocalisation.Get(getKey(nameof(UserOverrideDeleteTooltip)), "Delete");
    }
}
