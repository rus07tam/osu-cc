using osu.Framework.Localisation;

namespace osucc.Localisation
{
    public static class DownloadStrings
    {
        private const string prefix = "osucc.Localisation.Downloads";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString ButtonDefault => OsuCcLocalisation.Get(getKey(nameof(ButtonDefault)), "Download all favourites");

        public static LocalisableString ButtonFetching => OsuCcLocalisation.Get(getKey(nameof(ButtonFetching)), "Fetching favourites…");

        public static LocalisableString ButtonTooltip => OsuCcLocalisation.Get(getKey(nameof(ButtonTooltip)), "Enqueues a download for every beatmap set this profile has favourited, skipping the ones already in your library.");

        public static LocalisableString NoNewFavourites(int loaded, int skipped)
            => OsuCcLocalisation.Get(getKey(nameof(NoNewFavourites)), "no new favourites to download ({0} loaded, {1} already present)", loaded, skipped);

        public static LocalisableString Downloading(int enqueued)
            => OsuCcLocalisation.Get(getKey(nameof(Downloading)), "downloading {0} favourited beatmaps", enqueued);

        public static LocalisableString DownloadingSkipped(int enqueued, int skipped)
            => OsuCcLocalisation.Get(getKey(nameof(DownloadingSkipped)), "downloading {0} favourited beatmaps, {1} already present", enqueued, skipped);
    }
}
