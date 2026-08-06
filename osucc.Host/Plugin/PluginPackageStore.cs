using osucc.Core;
using SharpCompress.Common;
using SharpCompress.Readers;
using System.IO.Compression;

namespace osucc.Plugin
{
    /// <summary>
    /// Turns whatever was dropped into the plugins root into the per-plugin layout. Every
    /// plugin ends up in <c>&lt;root&gt;/&lt;id&gt;/</c>, keyed by the stable plugin id (not the
    /// archive/assembly name), so code, assets and the plugin's <c>plugin.ini</c> settings all
    /// share one folder. Archives are deleted after extraction — the next deploy drops a fresh
    /// archive that is re-extracted over the id-folder, so updates keep working.
    /// </summary>
    public static class PluginPackageStore
    {
        /// <summary>Name of the transient staging folder inside the plugins root.</summary>
        public const string StagingDirectoryName = ".staging";

        /// <summary>
        /// Prepares the plugins root for scanning: clears the staging folder, extracts every
        /// archive into <c>.staging/&lt;archive&gt;/</c> (then deletes it), and moves loose dlls
        /// into <c>.staging/&lt;dll&gt;/</c>. No-op when the root does not exist.
        /// </summary>
        public static void Prepare(string pluginsDirectory)
        {
            if (!Directory.Exists(pluginsDirectory))
                return;

            string staging = Path.Combine(pluginsDirectory, StagingDirectoryName);

            try
            {
                if (Directory.Exists(staging))
                    Directory.Delete(staging, recursive: true);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PluginPackageStore: failed to clear staging {staging}: {ex}");
            }

            try
            {
                Directory.CreateDirectory(staging);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PluginPackageStore: failed to create staging {staging}: {ex}");
                return;
            }

            foreach (string archive in Directory.GetFiles(pluginsDirectory).Where(isArchive))
            {
                string target = Path.Combine(staging, Path.GetFileNameWithoutExtension(archive));

                try
                {
                    Directory.CreateDirectory(target);
                    extractArchive(archive, target);
                    File.Delete(archive);
                    TimingLog.Info($"PluginPackageStore: extracted {Path.GetFileName(archive)} -> {target} (archive deleted)");
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"PluginPackageStore: failed to extract {archive}: {ex}");
                }
            }

            foreach (string dll in Directory.GetFiles(pluginsDirectory, "*.dll"))
            {
                string target = Path.Combine(staging, Path.GetFileNameWithoutExtension(dll));

                try
                {
                    Directory.CreateDirectory(target);
                    File.Move(dll, Path.Combine(target, Path.GetFileName(dll)));
                    TimingLog.Info($"PluginPackageStore: staged {Path.GetFileName(dll)} -> {target}");
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"PluginPackageStore: failed to stage {dll}: {ex}");
                }
            }

            removeStaleFolders(pluginsDirectory, staging);
        }

        /// <summary>
        /// Deletes the payload folders of plugins marked for deletion. Runs at startup before
        /// anything is loaded (so the dlls are still unlocked, which matters on Windows) and
        /// before <see cref="Prepare"/>, so a freshly dropped archive still re-installs the plugin.
        /// Returns the ids whose folder is gone (deleted or already missing); folders that could
        /// not be deleted are left out so their deletion marker survives.
        /// </summary>
        public static List<string> RemovePendingDeletes(string pluginsDirectory, IEnumerable<string> deletedIds)
        {
            var removed = new List<string>();

            foreach (string id in deletedIds)
            {
                string folder = Path.Combine(pluginsDirectory, id);

                try
                {
                    if (Directory.Exists(folder))
                    {
                        Directory.Delete(folder, recursive: true);
                        TimingLog.Info($"PluginPackageStore: removed deleted plugin folder {folder}");
                    }

                    removed.Add(id);
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"PluginPackageStore: failed to remove deleted plugin folder {folder}: {ex}");
                }
            }

            return removed;
        }

        /// <summary>
        /// Deletes old-scheme payload folders that have a same-named fresh copy now in staging
        /// (e.g. <c>example\</c> after <c>example.zip</c> was extracted). Runs here,
        /// before anything is loaded, while the dlls are still unlocked. Folders with a
        /// <c>plugin.ini</c> are left alone (settings survive a scheme migration).
        /// </summary>
        private static void removeStaleFolders(string pluginsDirectory, string staging)
        {
            foreach (string folder in Directory.GetDirectories(pluginsDirectory))
            {
                if (IsUnderStaging(folder, pluginsDirectory))
                    continue;

                if (File.Exists(Path.Combine(folder, "plugin.ini")))
                    continue;

                string stagedCopy = Path.Combine(staging, Path.GetFileName(folder));

                if (!Directory.Exists(stagedCopy))
                    continue;

                try
                {
                    Directory.Delete(folder, recursive: true);
                    TimingLog.Info($"PluginPackageStore: removed stale payload folder {folder}");
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"PluginPackageStore: failed to remove stale payload folder {folder}: {ex}");
                }
            }
        }

        /// <summary>
        /// Moves every staged plugin payload into its <c>&lt;root&gt;/&lt;id&gt;/</c> folder
        /// (preserving existing files such as <c>plugin.ini</c>), clears staging, and removes
        /// orphaned payload folders left over from an older naming scheme.
        /// </summary>
        public static void Finalize(IEnumerable<PluginEntry> entries, string pluginsDirectory)
        {
            var stagedEntries = entries
                                .Where(e => IsUnderStaging(e.Directory, pluginsDirectory))
                                .GroupBy(e => e.Directory)
                                .ToList();

            foreach (var group in stagedEntries)
            {
                string stagingFolder = group.Key;

                // Several plugins may share one staging folder (their auxiliary dlls travel
                // together); the first non-empty id wins as the target.
                string id = group.Select(e => e.Id).FirstOrDefault(i => i.Length > 0) ?? Path.GetFileName(stagingFolder);
                string target = Path.Combine(pluginsDirectory, id);

                try
                {
                    Directory.CreateDirectory(target);

                    foreach (string file in Directory.GetFiles(stagingFolder))
                        File.Move(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);

                    foreach (var entry in group)
                    {
                        entry.Directory = target;
                        entry.IconPath = repointIcon(entry.IconPath, stagingFolder, target);
                    }

                    TimingLog.Info($"PluginPackageStore: '{id}' payload -> {target}");
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"PluginPackageStore: failed to finalize {stagingFolder} -> {target}: {ex}");
                }
            }

            string stagingPath = Path.Combine(pluginsDirectory, StagingDirectoryName);

            try
            {
                if (Directory.Exists(stagingPath))
                    Directory.Delete(stagingPath, recursive: true);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PluginPackageStore: failed to remove staging {stagingPath}: {ex}");
            }

            removeOrphanedFolders(entries, pluginsDirectory);
        }

        /// <summary>
        /// Deletes folders in the plugins root that look like an old-scheme payload folder
        /// (contain a dll, hold no <c>plugin.ini</c>, and match no plugin id-folder). Settings
        /// folders are never touched, so user data survives a scheme migration.
        /// </summary>
        private static void removeOrphanedFolders(IEnumerable<PluginEntry> entries, string pluginsDirectory)
        {
            var targetIds = new HashSet<string>(entries.Where(e => e.Id.Length > 0).Select(e => e.Id));
            var entryFolders = new HashSet<string>(entries.Where(e => e.Directory.Length > 0).Select(e => Path.GetFullPath(e.Directory)), StringComparer.OrdinalIgnoreCase);

            foreach (string folder in Directory.GetDirectories(pluginsDirectory))
            {
                string name = Path.GetFileName(folder);

                if (name == StagingDirectoryName)
                    continue;

                if (targetIds.Contains(name))
                    continue;

                // Never delete a folder an entry currently points at (e.g. a manually dropped
                // old-scheme folder that was loaded before finalize repointed it).
                if (entryFolders.Contains(Path.GetFullPath(folder)))
                    continue;

                bool looksLikePayload = Directory.GetFiles(folder, "*.dll").Length != 0 && !File.Exists(Path.Combine(folder, "plugin.ini"));

                if (!looksLikePayload)
                    continue;

                try
                {
                    Directory.Delete(folder, recursive: true);
                    TimingLog.Info($"PluginPackageStore: removed orphaned folder {folder}");
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"PluginPackageStore: failed to remove orphaned folder {folder}: {ex}");
                }
            }
        }

        /// <summary>Whether <paramref name="directory"/> sits inside the plugins root's staging folder.</summary>
        public static bool IsUnderStaging(string directory, string pluginsDirectory)
        {
            string staging = Path.Combine(pluginsDirectory, StagingDirectoryName);

            if (directory.Length == 0)
                return false;

            string fullDirectory = Path.GetFullPath(directory);
            string fullStaging = Path.GetFullPath(staging);
            return fullDirectory.StartsWith(fullStaging, StringComparison.OrdinalIgnoreCase)
                   && fullDirectory.Length > fullStaging.Length;
        }

        private static string? repointIcon(string? iconPath, string fromFolder, string toFolder)
        {
            if (string.IsNullOrEmpty(iconPath))
                return null;

            string fullIcon = Path.GetFullPath(iconPath);
            string fullFrom = Path.GetFullPath(fromFolder);

            if (!fullIcon.StartsWith(fullFrom, StringComparison.OrdinalIgnoreCase))
                return iconPath;

            string relative = fullIcon[fullFrom.Length..].TrimStart('\\', '/');
            return Path.Combine(toFolder, relative);
        }

        private static bool isArchive(string path)
        {
            string extension = Path.GetExtension(path);
            return extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) || extension.Equals(".rar", StringComparison.OrdinalIgnoreCase);
        }

        private static void extractArchive(string archivePath, string target)
        {
            if (Path.GetExtension(archivePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ZipFile.ExtractToDirectory(archivePath, target, overwriteFiles: true);
                return;
            }

            // SharpCompress auto-detects the format (rar, 7z, tar, …) via the streaming reader.
            using (var reader = ReaderFactory.OpenReader(archivePath, new ReaderOptions()))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                        reader.WriteEntryToDirectory(target, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                }
            }
        }
    }
}
