using System;

namespace osucc.Plugin
{
    /// <summary>Type/target of a declared plugin dependency.</summary>
    public enum PluginDependencyKind
    {
        /// <summary>Host assembly / client runtime dependency (osucc.Api, osucc.Host, osu.Game, osu.Framework).</summary>
        Host,

        /// <summary>Another osu!cc plugin (referenced by plugin ID or NuGet package ID).</summary>
        Plugin,

        /// <summary>Bundled third-party or native assembly packaged in the plugin archive.</summary>
        Bundled,
    }

    /// <summary>
    /// Describes a declared dependency required by a plugin.
    /// </summary>
    public class PluginDependencyDeclaration
    {
        /// <summary>Target dependency identifier (e.g. "osucc", "osu.Game", "fake-supporter", "Newtonsoft.Json.dll").</summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>Kind of dependency.</summary>
        public PluginDependencyKind Kind { get; init; } = PluginDependencyKind.Plugin;

        /// <summary>Minimum required version (inclusive), or <c>null</c> if unbounded.</summary>
        public string? MinVersion { get; init; }

        /// <summary>Maximum required version (inclusive), or <c>null</c> if unbounded.</summary>
        public string? MaxVersion { get; init; }

        public PluginDependencyDeclaration()
        {
        }

        public PluginDependencyDeclaration(string id, PluginDependencyKind kind = PluginDependencyKind.Plugin, string? minVersion = null, string? maxVersion = null)
        {
            Id = id;
            Kind = kind;
            MinVersion = string.IsNullOrWhiteSpace(minVersion) ? null : minVersion.Trim();
            MaxVersion = string.IsNullOrWhiteSpace(maxVersion) ? null : maxVersion.Trim();
        }

        /// <summary>Encodes this declaration into a compact string representation for assembly attributes.</summary>
        public string Encode()
        {
            string kindStr = Kind switch
            {
                PluginDependencyKind.Host => "host",
                PluginDependencyKind.Bundled => "bundle",
                _ => "plugin",
            };

            return $"{kindStr}:{Id}|{MinVersion ?? string.Empty}|{MaxVersion ?? string.Empty}";
        }

        /// <summary>Decodes a compact string representation back into a declaration.</summary>
        public static PluginDependencyDeclaration? Decode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            string s = raw.Trim();
            PluginDependencyKind kind = PluginDependencyKind.Plugin;

            if (s.StartsWith("host:", StringComparison.OrdinalIgnoreCase))
            {
                kind = PluginDependencyKind.Host;
                s = s[5..];
            }
            else if (s.StartsWith("bundle:", StringComparison.OrdinalIgnoreCase))
            {
                kind = PluginDependencyKind.Bundled;
                s = s[7..];
            }
            else if (s.StartsWith("plugin:", StringComparison.OrdinalIgnoreCase))
            {
                kind = PluginDependencyKind.Plugin;
                s = s[7..];
            }

            string[] parts = s.Split('|');
            string id = parts[0].Trim();
            string? minVer = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1].Trim() : null;
            string? maxVer = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2].Trim() : null;

            return new PluginDependencyDeclaration(id, kind, minVer, maxVer);
        }

        public override string ToString()
        {
            if (MinVersion != null && MaxVersion != null)
                return $"{Id} ({MinVersion} - {MaxVersion})";
            if (MinVersion != null)
                return $"{Id} (>= {MinVersion})";
            if (MaxVersion != null)
                return $"{Id} (<= {MaxVersion})";

            return Id;
        }
    }
}
