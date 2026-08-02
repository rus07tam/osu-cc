using System;

namespace osucc.Plugin
{
    /// <summary>
    /// Describes a discovered plugin: metadata (from <see cref="OsuCcPluginAttribute"/>), the live
    /// instance and load/attach status. Consumed by the plugins overlay.
    /// </summary>
    public class PluginEntry
    {
        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string? Author { get; init; }

        public string? Description { get; init; }

        public string Version { get; init; } = string.Empty;

        public string? IconResource { get; init; }

        /// <summary>
        /// Absolute path of an <c>icon.*</c> / <c>image.*</c> file in the plugin folder (preferred
        /// over <see cref="IconResource"/>). Updated when the payload moves into the id-folder.
        /// </summary>
        public string? IconPath { get; internal set; }

        public int Priority { get; internal set; }

        /// <summary>Directory the plugin DLL + assets live in. Updated when the payload moves into the id-folder.</summary>
        public string Directory { get; internal set; } = string.Empty;

        /// <summary>The live plugin instance, <c>null</c> if discovery/load failed.</summary>
        public IOsuCcPlugin? Plugin { get; internal set; }

        /// <summary>The host bound to this plugin (kept so its config stays alive).</summary>
        public PluginHost? Host { get; internal set; }

        /// <summary>Set when discovery or load threw.</summary>
        public Exception? LoadError { get; internal set; }

        /// <summary>
        /// Whether the plugin is enabled. Disabled plugins are discovered but not loaded (no
        /// <see cref="IOsuCcPlugin.Load"/>, no patches, no UI contributions); they stay listed so
        /// they can be re-enabled. Persisted by <see cref="PluginManager"/>; changes apply on the
        /// next launch.
        /// </summary>
        public bool Enabled { get; internal set; } = true;

        public bool Loaded => Plugin != null && LoadError == null;

        /// <summary>Whether <see cref="IOsuCcPlugin.AttachToGame"/> has been called successfully.</summary>
        public bool Attached { get; internal set; }

        /// <summary>
        /// Whether deletion was confirmed. The payload folder is removed on the next launch
        /// (the loaded dll cannot be deleted mid-session on Windows); until then the plugin
        /// stays loaded but is shown as non-interactive in the plugins overlay.
        /// </summary>
        public bool PendingDelete { get; internal set; }

        /// <summary>
        /// Overlay status, resolved in order: pending deletion wins, then errors, then the
        /// enabled/disabled states (a plugin toggled off while still running is "pending
        /// disable", one disabled from the start is simply "disabled").
        /// </summary>
        public PluginStatus Status
        {
            get
            {
                if (PendingDelete)
                    return PluginStatus.PendingDelete;

                if (LoadError != null)
                    return PluginStatus.Error;

                if (!Enabled)
                    return Loaded ? PluginStatus.PendingDisable : PluginStatus.Disabled;

                return Loaded ? PluginStatus.Active : PluginStatus.PendingEnable;
            }
        }
    }
}
