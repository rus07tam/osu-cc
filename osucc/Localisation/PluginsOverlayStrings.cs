using osu.Framework.Localisation;

namespace osucc.Localisation
{
    public static class PluginsOverlayStrings
    {
        private const string prefix = "osucc.Localisation.Plugins";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString OverlayTitle => OsuCcLocalisation.Get(getKey(nameof(OverlayTitle)), "osu!cc plugins");

        public static LocalisableString OverlayDescription => OsuCcLocalisation.Get(getKey(nameof(OverlayDescription)), "Loaded plugins and their status");

        public static LocalisableString EmptyState => OsuCcLocalisation.Get(getKey(nameof(EmptyState)), "No plugins found. Drop plugin archives (*.zip / *.rar) into the osu-cc \"plugins\" folder (the game data folder's osu-cc directory).");

        public static LocalisableString OrderChanged => OsuCcLocalisation.Get(getKey(nameof(OrderChanged)), "Plugin order changed (applies on next launch)");

        public static LocalisableString ConfirmDialogFailed => OsuCcLocalisation.Get(getKey(nameof(ConfirmDialogFailed)), "Could not open the confirmation dialog.");

        public static LocalisableString DeleteTitle => OsuCcLocalisation.Get(getKey(nameof(DeleteTitle)), "Delete plugin?");

        public static LocalisableString DeleteBody(LocalisableString name)
            => OsuCcLocalisation.Get(getKey(nameof(DeleteBody)), "\"{0}\" will be removed on the next launch. Its files and settings will be deleted. This cannot be undone.", name);

        public static LocalisableString DeleteConfirmed(LocalisableString name)
            => OsuCcLocalisation.Get(getKey(nameof(DeleteConfirmed)), "Plugin \"{0}\" will be removed on the next launch", name);

        public static LocalisableString StatusActive => OsuCcLocalisation.Get(getKey(nameof(StatusActive)), "Loaded and attached");

        public static LocalisableString StatusPendingEnable => OsuCcLocalisation.Get(getKey(nameof(StatusPendingEnable)), "Will be enabled (next launch)");

        public static LocalisableString StatusPendingDisable => OsuCcLocalisation.Get(getKey(nameof(StatusPendingDisable)), "Will be disabled (next launch)");

        public static LocalisableString StatusPendingDelete => OsuCcLocalisation.Get(getKey(nameof(StatusPendingDelete)), "Will be deleted (next launch)");

        public static LocalisableString StatusDisabled => OsuCcLocalisation.Get(getKey(nameof(StatusDisabled)), "Disabled");

        public static LocalisableString StatusFailed => OsuCcLocalisation.Get(getKey(nameof(StatusFailed)), "Failed to load");

        public static LocalisableString StatusFailedWithError(string error)
            => OsuCcLocalisation.Get(getKey(nameof(StatusFailedWithError)), "Failed to load: {0}", error);

        public static LocalisableString MoveUp => OsuCcLocalisation.Get(getKey(nameof(MoveUp)), "Move up");

        public static LocalisableString MoveDown => OsuCcLocalisation.Get(getKey(nameof(MoveDown)), "Move down");

        public static LocalisableString DeletePluginTooltip => OsuCcLocalisation.Get(getKey(nameof(DeletePluginTooltip)), "Delete plugin");

        public static LocalisableString PluginEnabled(LocalisableString name)
            => OsuCcLocalisation.Get(getKey(nameof(PluginEnabled)), "Plugin '{0}' enabled (applies on next launch)", name);

        public static LocalisableString PluginDisabled(LocalisableString name)
            => OsuCcLocalisation.Get(getKey(nameof(PluginDisabled)), "Plugin '{0}' disabled (applies on next launch)", name);
    }
}
