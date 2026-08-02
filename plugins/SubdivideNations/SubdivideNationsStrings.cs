using osu.Framework.Localisation;
using osucc.Localisation;

namespace SubdivideNations
{
    public static class SubdivideNationsStrings
    {
        private const string prefix = "subdivide-nations";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString Name => OsuCcLocalisation.Get($"{prefix}:name", "Subdivide Nations");

        public static LocalisableString Description => OsuCcLocalisation.Get($"{prefix}:description", "Shows each user's sub-national region on profiles and user cards.");

        public static LocalisableString ShowRegionsCaption => OsuCcLocalisation.Get(getKey(nameof(ShowRegionsCaption)), "Show regions");

        public static LocalisableString ShowRegionsHint => OsuCcLocalisation.Get(getKey(nameof(ShowRegionsHint)), "Display each user's sub-national region on profiles and user cards.");

        public static LocalisableString ShowFlagsCaption => OsuCcLocalisation.Get(getKey(nameof(ShowFlagsCaption)), "Region flags");

        public static LocalisableString ShowFlagsHint => OsuCcLocalisation.Get(getKey(nameof(ShowFlagsHint)), "Load region flag images next to the country flag (region name still shows when unavailable).");
    }
}
