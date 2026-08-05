using System.Diagnostics;
using System.Reflection;

namespace osucc.Common;

/// <summary>
/// Reads and compares versions without depending on any osu-cc assembly: the hook's own version
/// comes from the FileVersion of <c>osucc.dll</c>, while a GitHub tag (e.g. <c>v1.2.3</c>) may
/// carry a leading <c>v</c>. Numeric comparison when both parse, ordinal otherwise.
/// </summary>
public static class OsuCcVersionReader
{
    /// <summary>Reads the FileVersion of the given DLL, or <c>null</c> when it is missing.</summary>
    public static string? Read(string dllPath)
    {
        if (!File.Exists(dllPath))
            return null;

        try
        {
            return FileVersionInfo.GetVersionInfo(dllPath).FileVersion;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>True when <paramref name="a"/> is not older than <paramref name="b"/> (or they cannot be compared numerically).</summary>
    public static bool IsAtLeast(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;

        if (TryParse(a, out Version? parsedA) && TryParse(b, out Version? parsedB))
            return parsedA >= parsedB;

        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Normalizes a tag/version string to a comparable <see cref="Version"/>, handling a leading <c>v</c> and 2–4 components.</summary>
    public static bool TryParse(string? value, out Version? version)
    {
        version = null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string candidate = value.Trim();

        if (candidate.StartsWith('v') || candidate.StartsWith('V'))
            candidate = candidate[1..];

        // Version.TryParse requires at least one dot; turn "1" into "1.0".
        if (!candidate.Contains('.'))
            candidate += ".0";

        if (candidate.Count(c => c == '.') < 2)
            candidate += ".0";

        return Version.TryParse(candidate, out version);
    }
}
