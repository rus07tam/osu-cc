using System.IO.Compression;
using osucc.Common;

namespace OsuCcUpdater
{
    /// <summary>
    /// Extracts a runtime bundle (as produced by <c>PackRuntimeBundle</c> and shipped on GitHub
    /// releases: a zip with top-level <c>hook/</c> and <c>plugins/</c> folders) into the staging
    /// directory, replacing any previous staging. Live files are never touched.
    /// </summary>
    internal static class StagingWriter
    {
        public static void FromBundle(string bundleFile, string osuCcDirectory)
        {
            string staging = OsuCcLayout.StagingDirectory(osuCcDirectory);

            try
            {
                if (Directory.Exists(staging))
                    Directory.Delete(staging, recursive: true);
            }
            catch (IOException)
            {
                // best effort; the next write recreates it
            }

            using ZipArchive archive = ZipFile.OpenRead(bundleFile);

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                string[] parts = entry.FullName.Split('/');

                if (parts.Length != 2)
                    continue;

                string topLevel = parts[0];
                string fileName = parts[1];

                if (topLevel != OsuCcLayout.HookDirectoryName && topLevel != OsuCcLayout.PluginsDirectoryName)
                    continue;

                // Zip-slip guard: the entry must be a plain file name, not a path.
                if (Path.GetFileName(fileName) != fileName)
                    continue;

                string target = Path.Combine(staging, topLevel, fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.ExtractToFile(target, overwrite: true);
            }
        }
    }
}