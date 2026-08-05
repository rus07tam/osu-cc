using osucc.Common;

namespace osucc.App;

/// <summary>
/// Applies a staged update before launching: the updater plugin downloads/builds the next hook
/// and plugin archives into <c>&lt;osu-cc&gt;/staging</c> (never touching live files, which are
/// locked on Windows) and writes an <see cref="UpdateMarker"/>. On the next launch the marker's
/// payload replaces the live <c>hook</c> and <c>plugins</c> folders. A staging folder without a
/// readable marker is discarded without being applied.
/// </summary>
internal static class StagedUpdateApplier
{
    /// <summary>Applies the staged update described by the marker, if any. Never throws.</summary>
    public static void Apply(ResolvedPaths paths)
    {
        var marker = UpdateMarker.TryRead(paths.StagingDirectory);

        if (marker == null)
            return;

        string stagedHook = Path.Combine(paths.StagingDirectory, OsuCcLayout.HookDirectoryName);
        string stagedPlugins = Path.Combine(paths.StagingDirectory, OsuCcLayout.PluginsDirectoryName);

        try
        {
            bool anything = false;

            if (Directory.Exists(stagedHook) && Directory.EnumerateFileSystemEntries(stagedHook).Any())
            {
                Directory.CreateDirectory(paths.HookDirectory);
                copyDirectory(stagedHook, paths.HookDirectory);
                anything = true;
            }

            if (Directory.Exists(stagedPlugins))
            {
                Directory.CreateDirectory(paths.PluginsDirectory);

                foreach (string archive in Directory.GetFiles(stagedPlugins, "*.zip"))
                {
                    File.Copy(archive, Path.Combine(paths.PluginsDirectory, Path.GetFileName(archive)), overwrite: true);
                    anything = true;
                }
            }

            if (anything)
                Console.WriteLine($"Applied staged update v{marker.Version} ({marker.Source}).");
            else
                Console.WriteLine($"Staged update v{marker.Version} ({marker.Source}) carried no files; discarding.");
        }
        catch (Exception ex)
        {
            // A failed apply must not block the launch; the staging folder survives for a retry.
            Console.Error.WriteLine($"WARNING: could not apply staged update v{marker.Version}: {ex.Message}");
            return;
        }

        UpdateMarker.Clear(paths.StagingDirectory);
    }

    private static void copyDirectory(string source, string target)
    {
        foreach (string file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
    }
}
