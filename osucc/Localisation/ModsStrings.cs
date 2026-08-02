using osu.Framework.Localisation;

namespace osucc.Localisation
{
    public static class ModsStrings
    {
        private const string prefix = "osucc.Localisation.Mods";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString RandomModsButton => OsuCcLocalisation.Get(getKey(nameof(RandomModsButton)), "Random mods");

        public static LocalisableString ScoreSubmissionDisabled => OsuCcLocalisation.Get(getKey(nameof(ScoreSubmissionDisabled)), "Score submission is currently disabled — you can enable it in the osu!cc settings.");
    }
}
