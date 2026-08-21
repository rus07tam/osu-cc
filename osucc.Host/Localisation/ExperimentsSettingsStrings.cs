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
    }
}
