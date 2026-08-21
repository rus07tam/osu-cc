using System;
using System.Linq;

namespace osucc.Plugin
{
    /// <summary>
    /// Lightweight semantic and date-based version parser and comparer.
    /// Supports standard SemVer (Major.Minor.Patch[-prerelease]), .NET 2-4 component versions,
    /// and date-based osu.Game versions (e.g. 2024.1115.0[-lazer]).
    /// </summary>
    public sealed class PluginSemanticVersion : IComparable<PluginSemanticVersion>, IEquatable<PluginSemanticVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public int Build { get; }
        public string? Prerelease { get; }

        public PluginSemanticVersion(int major, int minor = 0, int patch = 0, int build = 0, string? prerelease = null)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Build = build;
            Prerelease = string.IsNullOrWhiteSpace(prerelease) ? null : prerelease.Trim();
        }

        public static bool TryParse(string? input, out PluginSemanticVersion? version)
        {
            version = null;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            string s = input.Trim();

            if (s.StartsWith('v') || s.StartsWith('V'))
                s = s[1..].Trim();

            string? prerelease = null;
            int hyphenIndex = s.IndexOf('-');

            if (hyphenIndex >= 0)
            {
                prerelease = s[(hyphenIndex + 1)..].Trim();
                s = s[..hyphenIndex].Trim();
            }

            int plusIndex = s.IndexOf('+');
            if (plusIndex >= 0)
            {
                s = s[..plusIndex].Trim();
            }

            string[] parts = s.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return false;

            if (!int.TryParse(parts[0], out int major))
                return false;

            int minor = 0;
            int patch = 0;
            int build = 0;

            if (parts.Length > 1 && !int.TryParse(parts[1], out minor))
                return false;

            if (parts.Length > 2 && !int.TryParse(parts[2], out patch))
                return false;

            if (parts.Length > 3 && !int.TryParse(parts[3], out build))
                return false;

            version = new PluginSemanticVersion(major, minor, patch, build, prerelease);
            return true;
        }

        public static PluginSemanticVersion Parse(string input)
        {
            if (!TryParse(input, out var version) || version == null)
                throw new FormatException($"Invalid version string: '{input}'");

            return version;
        }

        public int CompareTo(PluginSemanticVersion? other)
        {
            if (other is null)
                return 1;

            if (Major != other.Major)
                return Major.CompareTo(other.Major);

            if (Minor != other.Minor)
                return Minor.CompareTo(other.Minor);

            if (Patch != other.Patch)
                return Patch.CompareTo(other.Patch);

            if (Build != other.Build)
                return Build.CompareTo(other.Build);

            // A version without a prerelease tag has higher precedence than one with a prerelease tag.
            if (Prerelease == null && other.Prerelease != null)
                return 1;

            if (Prerelease != null && other.Prerelease == null)
                return -1;

            if (Prerelease != null && other.Prerelease != null)
                return string.Compare(Prerelease, other.Prerelease, StringComparison.OrdinalIgnoreCase);

            return 0;
        }

        public static bool operator ==(PluginSemanticVersion? left, PluginSemanticVersion? right)
            => left?.Equals(right) ?? right is null;

        public static bool operator !=(PluginSemanticVersion? left, PluginSemanticVersion? right)
            => !(left == right);

        public static bool operator <(PluginSemanticVersion? left, PluginSemanticVersion? right)
            => left is not null && (right is null ? false : left.CompareTo(right) < 0);

        public static bool operator <=(PluginSemanticVersion? left, PluginSemanticVersion? right)
            => left is null || (right is not null && left.CompareTo(right) <= 0);

        public static bool operator >(PluginSemanticVersion? left, PluginSemanticVersion? right)
            => left is not null && (right is null || left.CompareTo(right) > 0);

        public static bool operator >=(PluginSemanticVersion? left, PluginSemanticVersion? right)
            => left is null ? right is null : (right is null ? true : left.CompareTo(right) >= 0);

        public override bool Equals(object? obj) => obj is PluginSemanticVersion other && Equals(other);

        public bool Equals(PluginSemanticVersion? other)
            => other is not null && CompareTo(other) == 0;

        public override int GetHashCode()
            => HashCode.Combine(Major, Minor, Patch, Build, Prerelease?.ToLowerInvariant());

        public override string ToString()
        {
            string baseVer = Build > 0
                ? $"{Major}.{Minor}.{Patch}.{Build}"
                : $"{Major}.{Minor}.{Patch}";

            return Prerelease != null ? $"{baseVer}-{Prerelease}" : baseVer;
        }

        /// <summary>
        /// Evaluates whether the <paramref name="current"/> version satisfies the min, max and/or range constraints.
        /// </summary>
        public static bool IsSatisfied(string? current, string? minVersion, string? maxVersion, out string? failureReason)
        {
            failureReason = null;

            if (string.IsNullOrWhiteSpace(current))
            {
                failureReason = "Current version is not available";
                return false;
            }

            if (!TryParse(current, out var currentParsed) || currentParsed == null)
            {
                failureReason = $"Current version '{current}' could not be parsed";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(minVersion))
            {
                if (TryParse(minVersion, out var minParsed) && minParsed != null)
                {
                    if (currentParsed < minParsed)
                    {
                        failureReason = $"Requires version >= {minVersion} (current: {current})";
                        return false;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(maxVersion))
            {
                if (TryParse(maxVersion, out var maxParsed) && maxParsed != null)
                {
                    if (currentParsed > maxParsed)
                    {
                        failureReason = $"Requires version <= {maxVersion} (current: {current})";
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
