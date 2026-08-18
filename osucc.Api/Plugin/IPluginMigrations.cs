using System.Collections.Generic;

namespace osucc.Plugin
{
    /// <summary>
    /// A single data migration step. The host applies steps in <see cref="ToVersion"/> order
    /// whenever the plugin's persisted schema version is lower than
    /// <see cref="IPluginMigrations.SchemaVersion"/>; each step's result is persisted before the
    /// next one runs, so partial progress survives a crash.
    /// </summary>
    public interface IPluginMigration
    {
        /// <summary>The schema version this step produces when applied.</summary>
        int ToVersion { get; }

        /// <summary>Applies the step against the plugin's settings store; log progress through <paramref name="log"/>.</summary>
        void Apply(PluginSettings settings, Action<string> log);
    }

    /// <summary>
    /// Optional data-migration support a plugin can implement to version its persisted data.
    /// Migration steps run before <see cref="IOsuCcPlugin.AttachToGame"/>, so the plugin always
    /// reads current-schema data. Fresh installs skip migrations (their data is born up to date).
    /// </summary>
    public interface IPluginMigrations
    {
        /// <summary>The current data schema version of the plugin. Bump alongside new migration steps.</summary>
        int SchemaVersion { get; }

        /// <summary>Ordered migration steps; each must target a higher schema version than the previous one.</summary>
        IEnumerable<IPluginMigration> Migrations { get; }
    }
}
