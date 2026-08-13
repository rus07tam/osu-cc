using osu.Framework.Localisation;

namespace osucc.Localisation
{
    public static class ThemePreviewStrings
    {
        private const string prefix = "osucc.Localisation.ThemePreview";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString Title => OsuCcLocalisation.Get(getKey(nameof(Title)), "Theme preview");

        public static LocalisableString Description => OsuCcLocalisation.Get(getKey(nameof(Description)), "Live preview of the cosmetic UI theme without a restart.");

        public static LocalisableString ApplyButton => OsuCcLocalisation.Get(getKey(nameof(ApplyButton)), "Apply & restart");

        public static LocalisableString CancelButton => OsuCcLocalisation.Get(getKey(nameof(CancelButton)), "Cancel");

        public static LocalisableString ApplyFailed => OsuCcLocalisation.Get(getKey(nameof(ApplyFailed)), "Could not open the theme confirmation dialog.");
    }
}
