using System;
using System.IO;

namespace osucc.Launcher.Core
{
    public static class OsuCcPaths
    {
        public static string ResolveOsuDirectory(string? overridePath)
        {
            if (!string.IsNullOrEmpty(overridePath))
                return Path.GetFullPath(overridePath);

            if (OperatingSystem.IsWindows())
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osulazer", "current");

            if (OperatingSystem.IsMacOS())
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, "Applications", "osu!.app");
            }

            return resolveLinuxOsuDirectory()
                   ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "osu", "current");
        }

        private static string? resolveLinuxOsuDirectory()
        {
            string? onPath = resolveOnPath("osu!");

            if (onPath != null)
            {
                string executable = resolveSymlinks(onPath);
                string? directory = Path.GetDirectoryName(executable);

                if (directory != null && File.Exists(Path.Combine(directory, "osu!")))
                    return directory;
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            foreach (string candidate in new[]
            {
                Path.Combine(home, ".local", "share", "osu", "current"),
                Path.Combine(home, ".local", "share", "osu-lazer"),
                Path.Combine(home, ".local", "share", "osu-lazer-bin"),
                "/opt/osu-lazer",
                "/opt/osu-lazer-bin",
            })
            {
                if (File.Exists(Path.Combine(candidate, "osu!")))
                    return candidate;
            }

            return null;
        }

        private static string? resolveOnPath(string fileName)
        {
            string? pathValue = Environment.GetEnvironmentVariable("PATH");

            if (string.IsNullOrEmpty(pathValue))
                return null;

            foreach (string directory in pathValue.Split(Path.PathSeparator))
            {
                if (directory.Length == 0)
                    continue;

                string candidate = Path.Combine(directory, fileName);

                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private static string resolveSymlinks(string path)
        {
            try
            {
                return new FileInfo(path).ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? path;
            }
            catch (Exception)
            {
                return path;
            }
        }

        public static string ResolveExecutable(string osuDirectory)
        {
            string name = OperatingSystem.IsWindows() ? "osu!.exe" : "osu!";
            return Path.Combine(osuDirectory, name);
        }
    }
}
