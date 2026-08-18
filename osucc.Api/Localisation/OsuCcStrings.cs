using osu.Framework.Localisation;

namespace osucc.Localisation
{
    public static class OsuCcStrings
    {
        private const string prefix = "osucc.Localisation.Common";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString Delete => OsuCcLocalisation.Get(getKey(nameof(Delete)), "Delete");

        public static LocalisableString Cancel => OsuCcLocalisation.Get(getKey(nameof(Cancel)), "Cancel");

        public static LocalisableString UnknownAuthor => OsuCcLocalisation.Get(getKey(nameof(UnknownAuthor)), "unknown author");

        public static LocalisableString MoveLeft => OsuCcLocalisation.Get(getKey(nameof(MoveLeft)), "Move left");

        public static LocalisableString MoveRight => OsuCcLocalisation.Get(getKey(nameof(MoveRight)), "Move right");

        public static LocalisableString InitializationFailed(string errors)
            => OsuCcLocalisation.Get(getKey(nameof(InitializationFailed)), "initialization failed: {0}", errors);

        public static LocalisableString ClientLoaded(string name)
            => OsuCcLocalisation.Get(getKey(nameof(ClientLoaded)), "{0} loaded", name);

        public static LocalisableString PluginsLoaded(int loaded, int total)
            => OsuCcLocalisation.Get(getKey(nameof(PluginsLoaded)), "plugins loaded: {0}/{1}", loaded, total);

        public static LocalisableString PluginsLoadedFailed(int loaded, int total, int failed)
            => OsuCcLocalisation.Get(getKey(nameof(PluginsLoadedFailed)), "plugins loaded: {0}/{1}, {2} failed", loaded, total, failed);
    }
}
