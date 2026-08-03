using osu.Framework.Localisation;
using osucc.Localisation;

namespace UsernameVisuals
{
    public static class UsernameVisualsStrings
    {
        private const string prefix = "username-visuals";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString Name => OsuCcLocalisation.Get($"{prefix}:name", "Username Visuals");

        public static LocalisableString Description => OsuCcLocalisation.Get($"{prefix}:description", "Username visuals plus an own-username display override (custom text / hide).");

        public static LocalisableString GradientEnabledCaption => OsuCcLocalisation.Get(getKey(nameof(GradientEnabledCaption)), "Username visuals");

        public static LocalisableString GradientEnabledHint => OsuCcLocalisation.Get(getKey(nameof(GradientEnabledHint)), "Render usernames with a horizontal gradient everywhere.");

        public static LocalisableString SelfPaletteCaption => OsuCcLocalisation.Get(getKey(nameof(SelfPaletteCaption)), "My username");

        public static LocalisableString SelfPaletteHint => OsuCcLocalisation.Get(getKey(nameof(SelfPaletteHint)), "Gradient colours for your own username.");

        public static LocalisableString OthersPaletteCaption => OsuCcLocalisation.Get(getKey(nameof(OthersPaletteCaption)), "Everyone else");

        public static LocalisableString OthersPaletteHint => OsuCcLocalisation.Get(getKey(nameof(OthersPaletteHint)), "Gradient colours for other users.");

        public static LocalisableString ReplaceEnabledCaption => OsuCcLocalisation.Get(getKey(nameof(ReplaceEnabledCaption)), "Replace my username");

        public static LocalisableString ReplaceEnabledHint => OsuCcLocalisation.Get(getKey(nameof(ReplaceEnabledHint)), "Show a custom text instead of your own username everywhere.");

        public static LocalisableString DisplayNameCaption => OsuCcLocalisation.Get(getKey(nameof(DisplayNameCaption)), "Display name");

        public static LocalisableString DisplayNamePlaceholder => OsuCcLocalisation.Get(getKey(nameof(DisplayNamePlaceholder)), "Player");

        public static LocalisableString HideEnabledCaption => OsuCcLocalisation.Get(getKey(nameof(HideEnabledCaption)), "Hide my username");

        public static LocalisableString HideEnabledHint => OsuCcLocalisation.Get(getKey(nameof(HideEnabledHint)), "Replace your own username with a white block (takes precedence over the custom text).");

        public static LocalisableString UserOverridesSectionCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverridesSectionCaption)), "Per-user overrides");

        public static LocalisableString UserOverrideIdCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverrideIdCaption)), "User ID");

        public static LocalisableString UserOverrideIdPlaceholder => OsuCcLocalisation.Get(getKey(nameof(UserOverrideIdPlaceholder)), "osu! user ID");

        public static LocalisableString UserOverridePaletteCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverridePaletteCaption)), "Gradient colours");

        public static LocalisableString UserOverridePaletteHint => OsuCcLocalisation.Get(getKey(nameof(UserOverridePaletteHint)), "Optional gradient for this user's username.");

        public static LocalisableString UserOverrideNameCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverrideNameCaption)), "Display name");

        public static LocalisableString UserOverrideNamePlaceholder => OsuCcLocalisation.Get(getKey(nameof(UserOverrideNamePlaceholder)), "Optional custom name");

        public static LocalisableString UserOverrideHideCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverrideHideCaption)), "Hide username");

        public static LocalisableString UserOverrideHideHint => OsuCcLocalisation.Get(getKey(nameof(UserOverrideHideHint)), "Replace this user's username with a solid block.");

        public static LocalisableString UserOverrideApplyButtonText => OsuCcLocalisation.Get(getKey(nameof(UserOverrideApplyButtonText)), "Apply");

        public static LocalisableString UserOverridesListCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverridesListCaption)), "Current overrides");

        public static LocalisableString NoUserOverrides => OsuCcLocalisation.Get(getKey(nameof(NoUserOverrides)), "No per-user overrides yet.");

        public static LocalisableString UserOverrideEditTooltip => OsuCcLocalisation.Get(getKey(nameof(UserOverrideEditTooltip)), "Edit");

        public static LocalisableString UserOverrideDeleteTooltip => OsuCcLocalisation.Get(getKey(nameof(UserOverrideDeleteTooltip)), "Delete");
    }
}
