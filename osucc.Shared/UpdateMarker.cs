using System.Text.Json;

namespace osucc.Common;

/// <summary>
/// The contract between the updater plugin (which stages a next build) and the launcher (which
/// applies it before launching osu!): a JSON marker in the staging folder describing what was
/// staged. The launcher only applies a staging folder that carries a readable marker, so a
/// half-written download or a cancelled build is never applied.
/// </summary>
public sealed record UpdateMarker(string Version, string Source, string StagedAt)
{
    /// <summary>
    /// Reads the marker from the given staging folder, or <c>null</c> when it is absent or
    /// unreadable (a corrupt marker is treated as absent; it is removed on read).
    /// </summary>
    public static UpdateMarker? TryRead(string stagingDirectory)
    {
        string markerPath = OsuCcLayout.UpdateMarkerPath(stagingDirectory);

        if (!File.Exists(markerPath))
            return null;

        try
        {
            return JsonSerializer.Deserialize<UpdateMarker>(File.ReadAllText(markerPath));
        }
        catch (Exception)
        {
            // A corrupt marker must never block a launch; discard it and let the staged files be
            // removed with the rest of the staging folder.
            TryDelete(stagingDirectory);
            return null;
        }
    }

    /// <summary>Writes the marker into the given staging folder.</summary>
    public static void Write(string stagingDirectory, UpdateMarker marker)
    {
        Directory.CreateDirectory(stagingDirectory);
        File.WriteAllText(OsuCcLayout.UpdateMarkerPath(stagingDirectory), JsonSerializer.Serialize(marker));
    }

    /// <summary>Removes the staging folder (marker and payload) entirely.</summary>
    public static void Clear(string stagingDirectory)
        => TryDelete(stagingDirectory);

    private static void TryDelete(string stagingDirectory)
    {
        try
        {
            if (Directory.Exists(stagingDirectory))
                Directory.Delete(stagingDirectory, recursive: true);
        }
        catch (Exception)
        {
            // Best-effort: a leftover staging folder is harmless, the next staged update replaces it.
        }
    }
}
