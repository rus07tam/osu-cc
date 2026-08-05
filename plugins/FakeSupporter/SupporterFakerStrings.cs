using osu.Framework.Localisation;
using osucc.Localisation;

namespace FakeSupporter
{
    public static class SupporterFakerStrings
    {
        private const string prefix = "fake-supporter";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString Name => OsuCcLocalisation.Get($"{prefix}:name", "Fake Supporter");

        public static LocalisableString Description => OsuCcLocalisation.Get($"{prefix}:description", "Fakes the current player's osu!supporter tag (level 1–10) everywhere, plus per-user supporter overrides and a public rule API.");

        public static LocalisableString EnabledCaption => OsuCcLocalisation.Get(getKey(nameof(EnabledCaption)), "Fake osu! supporter");

        public static LocalisableString EnabledHint => OsuCcLocalisation.Get(getKey(nameof(EnabledHint)), "Visually shows the current player with an osu!supporter tag (with the chosen level) everywhere — profile, leaderboards, scores, chat. Local cosmetic only: nothing is sent to the servers. Per-user overrides and plugin rules apply even while this is off.");

        public static LocalisableString LevelCaption => OsuCcLocalisation.Get(getKey(nameof(LevelCaption)), "Fake supporter level");

        public static LocalisableString LevelHint => OsuCcLocalisation.Get(getKey(nameof(LevelHint)), "How many hearts the fake supporter tag shows (1–10).");

        public static LocalisableString UserOverridesSectionCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverridesSectionCaption)), "Per-user overrides");

        public static LocalisableString UserOverrideIdCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverrideIdCaption)), "User ID");

        public static LocalisableString UserOverrideIdPlaceholder => OsuCcLocalisation.Get(getKey(nameof(UserOverrideIdPlaceholder)), "osu! user ID");

        public static LocalisableString UserOverrideModeCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverrideModeCaption)), "Override");

        public static LocalisableString UserOverrideModeHint => OsuCcLocalisation.Get(getKey(nameof(UserOverrideModeHint)), "Force this user's supporter state everywhere: show them with a supporter tag, or force the real non-supporter state.");

        public static LocalisableString UserOverrideLevelCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverrideLevelCaption)), "Level (1–10)");

        public static LocalisableString UserOverrideLevelPlaceholder => OsuCcLocalisation.Get(getKey(nameof(UserOverrideLevelPlaceholder)), "Optional heart level");

        public static LocalisableString UserOverrideApplyButtonText => OsuCcLocalisation.Get(getKey(nameof(UserOverrideApplyButtonText)), "Apply");

        public static LocalisableString UserOverridesListCaption => OsuCcLocalisation.Get(getKey(nameof(UserOverridesListCaption)), "Current overrides");

        public static LocalisableString NoUserOverrides => OsuCcLocalisation.Get(getKey(nameof(NoUserOverrides)), "No per-user overrides yet.");

        public static LocalisableString OverrideModeSupporter => OsuCcLocalisation.Get(getKey(nameof(OverrideModeSupporter)), "Supporter");

        public static LocalisableString OverrideModeNotSupporter => OsuCcLocalisation.Get(getKey(nameof(OverrideModeNotSupporter)), "Not supporter");

        public static LocalisableString UserOverrideEditTooltip => OsuCcLocalisation.Get(getKey(nameof(UserOverrideEditTooltip)), "Edit");

        public static LocalisableString UserOverrideDeleteTooltip => OsuCcLocalisation.Get(getKey(nameof(UserOverrideDeleteTooltip)), "Delete");
    }
}
