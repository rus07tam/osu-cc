using osu.Framework.Localisation;
using osucc.Localisation;

namespace OsuCcUpdater
{
    /// <summary>Localisable strings for the osu-cc updater plugin.</summary>
    public static class OsuCcUpdaterStrings
    {
        private const string prefix = "osucc-updater";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString Name => OsuCcLocalisation.Get($"{prefix}:name", "Updater");

        public static LocalisableString Description => OsuCcLocalisation.Get($"{prefix}:description", "Checks the official osu-cc repository for a new build and stages it for the next launch.");

        public static LocalisableString StatusCaption => OsuCcLocalisation.Get(getKey(nameof(StatusCaption)), "Status");

        public static LocalisableString SourceLabel => OsuCcLocalisation.Get(getKey(nameof(SourceLabel)), "Update source");

        public static LocalisableString SourceHint => OsuCcLocalisation.Get(getKey(nameof(SourceHint)), "GitHub release bundle downloads a prebuilt osucc-runtime archive; a local build clones the official repo and builds it with the .NET SDK.");

        public static LocalisableString SourceGithub => OsuCcLocalisation.Get(getKey(nameof(SourceGithub)), "GitHub release bundle");

        public static LocalisableString SourceLocalBuild => OsuCcLocalisation.Get(getKey(nameof(SourceLocalBuild)), "Local dotnet build");

        public static LocalisableString AutoCheckCaption => OsuCcLocalisation.Get(getKey(nameof(AutoCheckCaption)), "Check for updates automatically");

        public static LocalisableString AutoCheckHint => OsuCcLocalisation.Get(getKey(nameof(AutoCheckHint)), "Checks the GitHub release when the game starts and stages an update silently when one exists.");

        public static LocalisableString CheckButton => OsuCcLocalisation.Get(getKey(nameof(CheckButton)), "Check for updates and stage");

        public static LocalisableString BuildButton => OsuCcLocalisation.Get(getKey(nameof(BuildButton)), "Build and stage locally");

        public static LocalisableString TooltipMain => OsuCcLocalisation.Get(getKey(nameof(TooltipMain)), "osu-cc updater");

        public static LocalisableString TooltipSub => OsuCcLocalisation.Get(getKey(nameof(TooltipSub)), "Check for updates");

        // Dynamic messages are built with plain interpolation (LocalisableString.Format only takes
        // a plain string format), so they carry no {0} template in the localisation files.
        public static LocalisableString NotifyUpdateStaged(string version)
            => $"osu-cc update v{version} staged - it will be applied on the next launch via osucc";

        public static LocalisableString NotifyUpToDate(string version)
            => $"osu-cc is up to date (v{version})";

        public static LocalisableString NotifyFailed(string message)
            => $"Update check failed: {message}";
    }
}