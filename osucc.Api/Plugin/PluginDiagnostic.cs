using osu.Framework.Localisation;
using System;

namespace osucc.Plugin
{
    /// <summary>Severity level of a plugin diagnostic record.</summary>
    public enum PluginDiagnosticLevel
    {
        /// <summary>Informational notice or optional integration hint.</summary>
        Notice,

        /// <summary>Non-critical problem (e.g. missing optional plugin dependency, version warning).</summary>
        Warning,

        /// <summary>Critical error (e.g. lifecycle crash, patch failure, host incompatibility).</summary>
        Error,
    }

    /// <summary>Originating subsystem of a plugin diagnostic record.</summary>
    public enum PluginDiagnosticSource
    {
        /// <summary>General or plugin-emitted diagnostic.</summary>
        General,

        /// <summary>Lifecycle hook error (discovery, load, attach, migrations, uninstall).</summary>
        Lifecycle,

        /// <summary>Harmony patch resolution or execution error.</summary>
        Patch,

        /// <summary>Host or inter-plugin dependency validation issue.</summary>
        Dependency,

        /// <summary>Bundled assembly missing or version mismatch.</summary>
        Bundle,
    }

    /// <summary>
    /// Represents a structured diagnostic record (error, warning, notice) associated with a plugin.
    /// </summary>
    public class PluginDiagnostic
    {
        /// <summary>Severity level of the diagnostic.</summary>
        public PluginDiagnosticLevel Level { get; init; }

        /// <summary>Subsystem that produced the diagnostic.</summary>
        public PluginDiagnosticSource Source { get; init; } = PluginDiagnosticSource.General;

        /// <summary>Short localised message describing the diagnostic.</summary>
        public LocalisableString Message { get; init; }

        /// <summary>Optional technical details, description or hint.</summary>
        public string? Details { get; init; }

        /// <summary>Optional exception that caused the diagnostic.</summary>
        public Exception? Exception { get; init; }

        /// <summary>Optional target identifier (e.g. patch name, dependency ID, DLL filename).</summary>
        public string? Target { get; init; }

        /// <summary>Timestamp when this diagnostic was recorded.</summary>
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

        public PluginDiagnostic(
            PluginDiagnosticLevel level,
            LocalisableString message,
            string? details = null,
            Exception? exception = null,
            PluginDiagnosticSource source = PluginDiagnosticSource.General,
            string? target = null)
        {
            Level = level;
            Message = message;
            Details = details;
            Exception = exception;
            Source = source;
            Target = target;
            Timestamp = DateTimeOffset.Now;
        }

        public static PluginDiagnostic Notice(
            LocalisableString message,
            string? details = null,
            PluginDiagnosticSource source = PluginDiagnosticSource.General,
            string? target = null)
            => new(PluginDiagnosticLevel.Notice, message, details, null, source, target);

        public static PluginDiagnostic Warning(
            LocalisableString message,
            string? details = null,
            PluginDiagnosticSource source = PluginDiagnosticSource.General,
            string? target = null)
            => new(PluginDiagnosticLevel.Warning, message, details, null, source, target);

        public static PluginDiagnostic Error(
            LocalisableString message,
            Exception? exception = null,
            string? details = null,
            PluginDiagnosticSource source = PluginDiagnosticSource.General,
            string? target = null)
            => new(PluginDiagnosticLevel.Error, message, details, exception, source, target);
    }
}
