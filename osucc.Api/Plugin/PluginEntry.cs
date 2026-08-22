using System;

namespace osucc.Plugin
{
    /// <summary>
    /// Describes a discovered plugin: metadata (from <see cref="OsuCcPluginAttribute"/>), the live
    /// instance and load/attach status. Consumed by the plugins overlay.
    /// </summary>
    public class PluginEntry : IPluginMetadata
    {
        /// <summary>
        /// Raised whenever mutable overlay state (<see cref="Enabled"/>, <see cref="PendingDelete"/>)
        /// changes. Consumed by the plugins overlay so its cards restyle from the shared data instead
        /// of tracking UI behaviour.
        /// </summary>
        public event Action? StateChanged;

        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public IReadOnlyList<PluginAuthor> Authors { get; init; } = Array.Empty<PluginAuthor>();

        public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

        public IReadOnlyList<PluginDocument> Documents { get; init; } = Array.Empty<PluginDocument>();

        public string? Description { get; init; }

        /// <summary>
        /// Repository the plugin is published from (from <see cref="OsuCcPluginAttribute.Repository"/>),
        /// shown as a link in the plugins overlay when set.
        /// </summary>
        public string? Repository { get; init; }

        public string Version { get; init; } = string.Empty;

        public string? IconResource { get; init; }

        /// <summary>
        /// FontAwesome glyph name declared in <see cref="OsuCcPluginAttribute.Icon"/>. Available
        /// without a live plugin instance, so a disabled/errored plugin still shows its real icon.
        /// </summary>
        public string? Icon { get; init; }

        /// <summary>
        /// Absolute path of the plugin's image icon file in the plugin folder (preferred over
        /// <see cref="IconResource"/>). Updated when the payload moves into the id-folder.
        /// </summary>
        public string? IconPath { get; set; }

        public int Priority { get; set; }

        /// <summary>
        /// The osu!cc API version the plugin was built against

        /// </summary>


        /// <summary>
        /// Stable ids of plugins this plugin depends on (from <see cref="OsuCcPluginAttribute.DependsOn"/>).
        /// Dependencies load first; a missing/disabled dependency only logs a warning.
        /// </summary>
        public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();

        /// <summary>Directory the plugin DLL + assets live in. Updated when the payload moves into the id-folder.</summary>
        public string Directory { get; set; } = string.Empty;

        /// <summary>Declared structured dependencies.</summary>
        public IReadOnlyList<PluginDependencyDeclaration> DependencyDeclarations { get; init; } = Array.Empty<PluginDependencyDeclaration>();

        private readonly object diagnosticsLock = new();
        private readonly List<PluginDiagnostic> diagnostics = new();

        /// <summary>All diagnostic records (errors, warnings, notices) registered for this plugin.</summary>
        public IReadOnlyList<PluginDiagnostic> Diagnostics
        {
            get
            {
                lock (diagnosticsLock)
                    return diagnostics.ToArray();
            }
        }

        public int ErrorCount
        {
            get
            {
                lock (diagnosticsLock)
                    return diagnostics.Count(d => d.Level == PluginDiagnosticLevel.Error);
            }
        }

        public int WarningCount
        {
            get
            {
                lock (diagnosticsLock)
                    return diagnostics.Count(d => d.Level == PluginDiagnosticLevel.Warning);
            }
        }

        public int NoticeCount
        {
            get
            {
                lock (diagnosticsLock)
                    return diagnostics.Count(d => d.Level == PluginDiagnosticLevel.Notice);
            }
        }

        public void AddDiagnostic(PluginDiagnostic diagnostic)
        {
            lock (diagnosticsLock)
                diagnostics.Add(diagnostic);

            StateChanged?.Invoke();
        }

        public void ClearDiagnostics(PluginDiagnosticSource? source = null)
        {
            lock (diagnosticsLock)
            {
                if (source == null)
                    diagnostics.Clear();
                else
                    diagnostics.RemoveAll(d => d.Source == source.Value);
            }

            StateChanged?.Invoke();
        }

        /// <summary>The live plugin instance, <c>null</c> if discovery/load failed or the plugin is disabled.</summary>
        public OsuCcPlugin? Plugin { get; set; }

        /// <summary>
        /// The discovered <c>[OsuCcPlugin]</c> type, retained even for disabled or version-mismatch
        /// plugins so their metadata can be shown.
        /// </summary>
        public Type? PluginType { get; set; }

        /// <summary>The host bound to this plugin (kept so its config stays alive).</summary>
        public IOsuCcPluginHost? Host { get; set; }

        /// <summary>Set when discovery or load threw.</summary>
        public Exception? LoadError { get; set; }

        /// <summary>
        /// Whether the plugin is enabled. Disabled plugins are discovered but not loaded (no
        /// <see cref="OsuCcPlugin.Load"/>, no patches, no UI contributions); they stay listed so
        /// they can be re-enabled. Persisted by <see cref="PluginManager"/>; changes apply on the
        /// next launch.
        /// </summary>
        private bool enabled = true;

        public bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value)
                    return;

                enabled = value;
                StateChanged?.Invoke();
            }
        }

        public bool Loaded => Plugin != null && LoadError == null;

        /// <summary>Whether <see cref="OsuCcPlugin.AttachToGame"/> has been called successfully.</summary>
        public bool Attached { get; set; }

        /// <summary>
        /// Whether deletion was confirmed. The payload folder is removed on the next launch
        /// (the loaded dll cannot be deleted mid-session on Windows); until then the plugin
        /// stays loaded but is shown as non-interactive in the plugins overlay.
        /// </summary>
        private bool pendingDelete;

        public bool PendingDelete
        {
            get => pendingDelete;
            set
            {
                if (pendingDelete == value)
                    return;

                pendingDelete = value;
                StateChanged?.Invoke();
            }
        }

        /// <summary>Whether the plugin was enabled at the start of the current session or after hot reload.</summary>
        public bool InitialEnabled { get; set; } = true;

        /// <summary>
        /// Overlay status, resolved in order: pending deletion wins, then errors, then
        /// pending state changes (if toggled during the current session), then active/disabled.
        /// </summary>
        public PluginStatus Status
        {
            get
            {
                if (PendingDelete)
                    return PluginStatus.PendingDelete;

                if (LoadError != null || (Enabled && ErrorCount > 0 && !Loaded))
                    return PluginStatus.Error;

                if (Enabled != InitialEnabled)
                    return Enabled ? PluginStatus.PendingEnable : PluginStatus.PendingDisable;

                return Loaded ? PluginStatus.Active : PluginStatus.Disabled;
            }
        }
    }
}
