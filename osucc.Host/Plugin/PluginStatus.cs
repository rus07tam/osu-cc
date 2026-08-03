namespace osucc.Plugin
{
    /// <summary>Lifecycle status of a plugin, shown as the status line in the plugins overlay.</summary>
    public enum PluginStatus
    {
        /// <summary>Loaded and attached to the game; fully active.</summary>
        Active,

        /// <summary>Enabled during this session but not loaded yet; will load on the next launch.</summary>
        PendingEnable,

        /// <summary>Disabled during this session while still running; will stop loading on the next launch.</summary>
        PendingDisable,

        /// <summary>Deletion confirmed; the payload folder is removed on the next launch.</summary>
        PendingDelete,

        /// <summary>Disabled — was not loaded this session.</summary>
        Disabled,

        /// <summary>Discovery or load failed.</summary>
        Error,
    }
}
