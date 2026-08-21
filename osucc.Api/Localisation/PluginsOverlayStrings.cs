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

        public static LocalisableString DeleteRestored(LocalisableString name)
            => OsuCcLocalisation.Get(getKey(nameof(DeleteRestored)), "Deletion of plugin \"{0}\" cancelled", name);

        public static LocalisableString CancelDelete => OsuCcLocalisation.Get(getKey(nameof(CancelDelete)), "Cancel deletion");

        public static LocalisableString OpenRepository => OsuCcLocalisation.Get(getKey(nameof(OpenRepository)), "Open repository");

        public static LocalisableString RepositoryOpenFailed(string url)
            => OsuCcLocalisation.Get(getKey(nameof(RepositoryOpenFailed)), "Could not open repository: {0}", url);

        public static LocalisableString ToggleEnabled => OsuCcLocalisation.Get(getKey(nameof(ToggleEnabled)), "Enable plugin");

        public static LocalisableString ToggleDisabled => OsuCcLocalisation.Get(getKey(nameof(ToggleDisabled)), "Disable plugin");

        public static LocalisableString ClearDataTitle => OsuCcLocalisation.Get(getKey(nameof(ClearDataTitle)), "Delete plugin data?");

        public static LocalisableString ClearDataBody(LocalisableString name)
            => OsuCcLocalisation.Get(getKey(nameof(ClearDataBody)), "\"{0}\" settings and data will be reset to defaults on the next launch. The plugin itself is not removed.", name);

        public static LocalisableString ClearDataConfirmed(LocalisableString name)
            => OsuCcLocalisation.Get(getKey(nameof(ClearDataConfirmed)), "Plugin \"{0}\" data cleared (applies on next launch)", name);

        public static LocalisableString DetailsActionsTitle => OsuCcLocalisation.Get(getKey(nameof(DetailsActionsTitle)), "Actions");

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
            => OsuCcLocalisation.Get(getKey(nameof(PluginEnabled)), "Plugin '{0}' enabled", name);

        public static LocalisableString PluginDisabled(LocalisableString name)
            => OsuCcLocalisation.Get(getKey(nameof(PluginDisabled)), "Plugin '{0}' disabled", name);

        public static LocalisableString DependenciesCaption => OsuCcLocalisation.Get(getKey(nameof(DependenciesCaption)), "Depends on:");

        public static LocalisableString UsedByCaption => OsuCcLocalisation.Get(getKey(nameof(UsedByCaption)), "Used by:");

        public static LocalisableString DependencyMissing => OsuCcLocalisation.Get(getKey(nameof(DependencyMissing)), "missing");

        public static LocalisableString DependencyDisabled => OsuCcLocalisation.Get(getKey(nameof(DependencyDisabled)), "disabled");

        public static LocalisableString NoPluginSettings => OsuCcLocalisation.Get(getKey(nameof(NoPluginSettings)), "This plugin has no settings.");

        public static LocalisableString SettingsOpenFailed(string error)
            => OsuCcLocalisation.Get(getKey(nameof(SettingsOpenFailed)), "Could not open plugin settings: {0}", error);

        public static LocalisableString DetailsId => OsuCcLocalisation.Get(getKey(nameof(DetailsId)), "ID");

        public static LocalisableString DetailsAuthor => OsuCcLocalisation.Get(getKey(nameof(DetailsAuthor)), "Author");

        public static LocalisableString DetailsTags => OsuCcLocalisation.Get(getKey(nameof(DetailsTags)), "Tags");

        public static LocalisableString SearchPlaceholder => OsuCcLocalisation.Get(getKey(nameof(SearchPlaceholder)), "Search by name, author, id or tag");

        public static LocalisableString SearchNoResults => OsuCcLocalisation.Get(getKey(nameof(SearchNoResults)), "No plugins match your search");

        public static LocalisableString DetailsVersion => OsuCcLocalisation.Get(getKey(nameof(DetailsVersion)), "Version");

        public static LocalisableString DetailsApiVersion => OsuCcLocalisation.Get(getKey(nameof(DetailsApiVersion)), "API version");

        public static LocalisableString DetailsPriority => OsuCcLocalisation.Get(getKey(nameof(DetailsPriority)), "Priority");

        public static LocalisableString DetailsSettingsTitle => OsuCcLocalisation.Get(getKey(nameof(DetailsSettingsTitle)), "Settings");

        public static LocalisableString DetailsRelationsNone => OsuCcLocalisation.Get(getKey(nameof(DetailsRelationsNone)), "None");

        public static LocalisableString InstalledTab => OsuCcLocalisation.Get(getKey(nameof(InstalledTab)), "Installed");

        public static LocalisableString BrowserTab => OsuCcLocalisation.Get(getKey(nameof(BrowserTab)), "Plugin Catalog");

        public static LocalisableString BrowserStubTitle => OsuCcLocalisation.Get(getKey(nameof(BrowserStubTitle)), "Plugin catalog coming soon");

        public static LocalisableString BrowserStubDescription => OsuCcLocalisation.Get(getKey(nameof(BrowserStubDescription)), "In future updates, you will be able to discover, browse and install community plugins directly from here.");

        public static LocalisableString DocumentFileNotFound(string path)
            => OsuCcLocalisation.Get(getKey(nameof(DocumentFileNotFound)), "Document file not found: {0}", path);

        public static LocalisableString DiagnosticsErrorCount(int count)
            => OsuCcLocalisation.Get(getKey(nameof(DiagnosticsErrorCount)), "{0} error(s)", count);

        public static LocalisableString DiagnosticsWarningCount(int count)
            => OsuCcLocalisation.Get(getKey(nameof(DiagnosticsWarningCount)), "{0} warning(s)", count);

        public static LocalisableString DiagnosticsNoticeCount(int count)
            => OsuCcLocalisation.Get(getKey(nameof(DiagnosticsNoticeCount)), "{0} notice(s)", count);

        public static LocalisableString DetailsDiagnosticsTitle => OsuCcLocalisation.Get(getKey(nameof(DetailsDiagnosticsTitle)), "Diagnostics");

        public static LocalisableString DiagnosticsEmpty => OsuCcLocalisation.Get(getKey(nameof(DiagnosticsEmpty)), "No issues detected");

        public static LocalisableString DiagnosticsSourceLifecycle => OsuCcLocalisation.Get(getKey(nameof(DiagnosticsSourceLifecycle)), "Lifecycle");

        public static LocalisableString DiagnosticsSourcePatch => OsuCcLocalisation.Get(getKey(nameof(DiagnosticsSourcePatch)), "Patch");

        public static LocalisableString DiagnosticsSourceDependency => OsuCcLocalisation.Get(getKey(nameof(DiagnosticsSourceDependency)), "Dependency");

        public static LocalisableString DiagnosticsSourceBundle => OsuCcLocalisation.Get(getKey(nameof(DiagnosticsSourceBundle)), "Bundled file");

        public static LocalisableString DiagnosticsSourceGeneral => OsuCcLocalisation.Get(getKey(nameof(DiagnosticsSourceGeneral)), "General");

        public static LocalisableString DependencyOsuCcOutdated(string required, string current)
            => OsuCcLocalisation.Get(getKey(nameof(DependencyOsuCcOutdated)), "Requires newer osu!cc (>= {0}, current: {1})", required, current);

        public static LocalisableString DependencyOsuCcTooNew(string required, string current)
            => OsuCcLocalisation.Get(getKey(nameof(DependencyOsuCcTooNew)), "Designed for older osu!cc (<= {0}, current: {1})", required, current);

        public static LocalisableString DependencyOsuGameOutdated(string required, string current)
            => OsuCcLocalisation.Get(getKey(nameof(DependencyOsuGameOutdated)), "Requires newer osu!lazer (>= {0}, current: {1})", required, current);

        public static LocalisableString DependencyOsuGameTooNew(string required, string current)
            => OsuCcLocalisation.Get(getKey(nameof(DependencyOsuGameTooNew)), "Designed for older osu!lazer (<= {0}, current: {1})", required, current);

        public static LocalisableString DependencyPluginMissingWithVersion(string id, string version)
            => OsuCcLocalisation.Get(getKey(nameof(DependencyPluginMissingWithVersion)), "Requires plugin \"{0}\" {1}", id, version);

        public static LocalisableString DependencyPluginDisabledNotice(string name)
            => OsuCcLocalisation.Get(getKey(nameof(DependencyPluginDisabledNotice)), "Optional integration with \"{0}\" is available if enabled", name);

        public static LocalisableString DependencyPluginVersionMismatch(string name, string current, string required)
            => OsuCcLocalisation.Get(getKey(nameof(DependencyPluginVersionMismatch)), "Plugin \"{0}\" version {1} is incompatible (expected {2})", name, current, required);

        public static LocalisableString DependencyBundledMissing(string dll)
            => OsuCcLocalisation.Get(getKey(nameof(DependencyBundledMissing)), "Bundled assembly \"{0}\" is missing", dll);

        public static LocalisableString DependencyBundledVersionMismatch(string dll, string current, string required)
            => OsuCcLocalisation.Get(getKey(nameof(DependencyBundledVersionMismatch)), "Bundled assembly \"{0}\" version {1} is incompatible (expected {2})", dll, current, required);
    }
}
