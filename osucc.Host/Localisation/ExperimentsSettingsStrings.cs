using osu.Framework.Localisation;

namespace osucc.Localisation
{
    public static class ExperimentsSettingsStrings
    {
        private const string prefix = "osucc.Localisation.Experiments";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString SubsectionHeader => OsuCcLocalisation.Get(getKey(nameof(SubsectionHeader)), "Experiments");

        public static LocalisableString LivePluginReloadingCaption => OsuCcLocalisation.Get(getKey(nameof(LivePluginReloadingCaption)), "Live plugin reloading");

        public static LocalisableString LivePluginReloadingHint => OsuCcLocalisation.Get(getKey(nameof(LivePluginReloadingHint)), "Allows enabling and disabling plugins at runtime without restarting the game. Highly experimental and unstable. Requires restart to change.");

        public static LocalisableString BypassHostDependencyCheckCaption => OsuCcLocalisation.Get(getKey(nameof(BypassHostDependencyCheckCaption)), "Bypass host dependency checks");

        public static LocalisableString BypassHostDependencyCheckHint => OsuCcLocalisation.Get(getKey(nameof(BypassHostDependencyCheckHint)), "Allows loading plugins even if their required osu!cc or osu!lazer version does not match the running client.");

        public static LocalisableString BypassPluginDependencyCheckCaption => OsuCcLocalisation.Get(getKey(nameof(BypassPluginDependencyCheckCaption)), "Bypass inter-plugin dependency checks");

        public static LocalisableString BypassPluginDependencyCheckHint => OsuCcLocalisation.Get(getKey(nameof(BypassPluginDependencyCheckHint)), "Suppresses version compatibility warnings and checks for dependencies between plugins.");
    }
}
