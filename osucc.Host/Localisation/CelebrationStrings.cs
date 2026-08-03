using osu.Framework.Localisation;

namespace osucc.Localisation
{
    public static class CelebrationStrings
    {
        private const string prefix = "osucc.Localisation.Celebrations";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString NewBestScore => OsuCcLocalisation.Get(getKey(nameof(NewBestScore)), "New best score!");

        public static LocalisableString BestScore(long score)
            => OsuCcLocalisation.Get(getKey(nameof(BestScore)), "{0:N0}", score);
    }
}
