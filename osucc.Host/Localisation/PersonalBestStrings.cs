using osu.Framework.Localisation;

namespace osucc.Localisation
{
    public static class PersonalBestStrings
    {
        private const string prefix = "osucc.Localisation.PersonalBest";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString Title => OsuCcLocalisation.Get(getKey(nameof(Title)), "New personal best!");

        public static LocalisableString Score(long score)
            => OsuCcLocalisation.Get(getKey(nameof(Score)), "{0:N0}", score);
    }
}
