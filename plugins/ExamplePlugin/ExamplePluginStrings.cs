using osu.Framework.Localisation;
using osucc.Localisation;

namespace ExamplePlugin
{
    public static class ExamplePluginStrings
    {
        private const string prefix = "example";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString Name => OsuCcLocalisation.Get($"{prefix}:name", "Example Plugin");

        public static LocalisableString Description => OsuCcLocalisation.Get($"{prefix}:description", "Demonstrates the osu!cc plugin API: toolbar button, notifications, celebrations, settings, a Harmony patch, lifecycle hooks and a data migration.");

        public static LocalisableString TooltipMain => OsuCcLocalisation.Get(getKey(nameof(TooltipMain)), "Example plugin");

        public static LocalisableString TooltipSub => OsuCcLocalisation.Get(getKey(nameof(TooltipSub)), "Shows a celebration");

        public static LocalisableString CelebrationTitle => OsuCcLocalisation.Get(getKey(nameof(CelebrationTitle)), "EXAMPLE PLUGIN");

        public static LocalisableString CelebrationSubtitle => OsuCcLocalisation.Get(getKey(nameof(CelebrationSubtitle)), "celebrations work from plugins");

        public static LocalisableString CelebrationsDisabled => OsuCcLocalisation.Get(getKey(nameof(CelebrationsDisabled)), "celebrations are disabled in the plugin settings");

        public static LocalisableString Attached => OsuCcLocalisation.Get(getKey(nameof(Attached)), "Example plugin attached");

        public static LocalisableString Installed => OsuCcLocalisation.Get(getKey(nameof(Installed)), "Example plugin installed");

        public static LocalisableString Uninstalled => OsuCcLocalisation.Get(getKey(nameof(Uninstalled)), "Example plugin uninstalled");

        public static LocalisableString Updated(string previous, string version)
            => OsuCcLocalisation.Get(getKey(nameof(Updated)), "Example plugin updated {0} -> {1}", previous, version);

        public static LocalisableString CelebrateCaption => OsuCcLocalisation.Get(getKey(nameof(CelebrateCaption)), "Celebrate from toolbar button");

        public static LocalisableString CelebrateHint => OsuCcLocalisation.Get(getKey(nameof(CelebrateHint)), "Whether the example toolbar button shows a full-screen celebration.");

        public static LocalisableString UsernameVisualsIntegrationCaption => OsuCcLocalisation.Get(getKey(nameof(UsernameVisualsIntegrationCaption)), "Username Visuals integration");

        public static LocalisableString UsernameVisualsIntegrationHint => OsuCcLocalisation.Get(getKey(nameof(UsernameVisualsIntegrationHint)), "Registers a demo colour gradient and a display-name rule through the Username Visuals plugin API; toggle to register or revoke them live.");

        public static LocalisableString DialogButton => OsuCcLocalisation.Get(getKey(nameof(DialogButton)), "Show confirmation dialog");

        public static LocalisableString DialogTitle => OsuCcLocalisation.Get(getKey(nameof(DialogTitle)), "Confirm example action");

        public static LocalisableString DialogBody => OsuCcLocalisation.Get(getKey(nameof(DialogBody)), "This confirmation is shown through the plugin dialog API (host.Confirm).");

        public static LocalisableString DialogConfirmed => OsuCcLocalisation.Get(getKey(nameof(DialogConfirmed)), "example confirmation confirmed");
    }
}
