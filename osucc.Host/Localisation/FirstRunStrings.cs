using osu.Framework.Localisation;

namespace osucc.Localisation
{
    public static class FirstRunStrings
    {
        private const string prefix = "osucc.Localisation.FirstRun";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString WelcomeTitle => OsuCcLocalisation.Get(getKey(nameof(WelcomeTitle)), "Welcome to osu!cc");

        public static LocalisableString UnderstandButton => OsuCcLocalisation.Get(getKey(nameof(UnderstandButton)), "I understand");

        public static LocalisableString BodyStart => OsuCcLocalisation.Get(getKey(nameof(BodyStart)), "osu!cc is ");

        public static LocalisableString BodyNotCheat => OsuCcLocalisation.Get(getKey(nameof(BodyNotCheat)), "not a cheat client");

        public static LocalisableString BodyNotCheatRest => OsuCcLocalisation.Get(getKey(nameof(BodyNotCheatRest)), " and will never contain cheats or similar functionality.");

        public static LocalisableString BodyUnstableStart => OsuCcLocalisation.Get(getKey(nameof(BodyUnstableStart)), "Due to how it works, it may behave ");

        public static LocalisableString BodyUnstable => OsuCcLocalisation.Get(getKey(nameof(BodyUnstable)), "unstable");

        public static LocalisableString BodyUnstableRest => OsuCcLocalisation.Get(getKey(nameof(BodyUnstableRest)), " or unexpectedly.");

        public static LocalisableString BodyRiskStart => OsuCcLocalisation.Get(getKey(nameof(BodyRiskStart)), "There is ");

        public static LocalisableString BodyNoGuarantee => OsuCcLocalisation.Get(getKey(nameof(BodyNoGuarantee)), "no guarantee");

        public static LocalisableString BodyRiskRest => OsuCcLocalisation.Get(getKey(nameof(BodyRiskRest)), " your account will not be banned — this client ");

        public static LocalisableString BodyViolatesRules => OsuCcLocalisation.Get(getKey(nameof(BodyViolatesRules)), "violates osu!'s rules");

        public static LocalisableString BodyRiskEnd => OsuCcLocalisation.Get(getKey(nameof(BodyRiskEnd)), ". Use at your own risk.");
    }
}
