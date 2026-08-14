using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace osucc.Data
{
    public class ScopedStorage : IOsuCcStorage
    {
        private readonly osu.Framework.Platform.Storage? diskStorage;
        private readonly Assembly? resourceAssembly;

        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public string Scope { get; }

        public ScopedStorage(string scope, osu.Framework.Platform.Storage? diskStorage, Assembly? resourceAssembly)
        {
            Scope = scope;
            this.diskStorage = diskStorage;
            this.resourceAssembly = resourceAssembly;
        }

        public T? ReadJson<T>(string path, T? defaultValue = null) where T : class
        {
            using var stream = GetStream(path, FileAccess.Read);
            if (stream == null)
                return defaultValue;

            try
            {
                return JsonSerializer.Deserialize<T>(stream, jsonOptions) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public void WriteJson<T>(string path, T data) where T : class
        {
            using var stream = GetStream(path, FileAccess.Write);
            if (stream != null)
            {
                JsonSerializer.Serialize(stream, data, jsonOptions);
            }
        }

        public Stream? GetStream(string path, FileAccess access = FileAccess.Read)
        {
            if (diskStorage != null)
            {
                if (access == FileAccess.Write)
                {
                    return diskStorage.GetStream(path, FileAccess.Write, FileMode.Create);
                }

                if (diskStorage.Exists(path))
                {
                    return diskStorage.GetStream(path, FileAccess.Read, FileMode.Open);
                }
            }

            if (access == FileAccess.Write)
                return null;

            if (resourceAssembly != null)
            {
                var resourceName = TryResolveResourceName(path);
                if (resourceName != null)
                {
                    return resourceAssembly.GetManifestResourceStream(resourceName);
                }
            }

            return null;
        }

        public bool Exists(string path)
        {
            if (diskStorage != null && diskStorage.Exists(path)) return true;

            if (resourceAssembly != null)
            {
                return TryResolveResourceName(path) != null;
            }

            return false;
        }

        public IEnumerable<string> GetFiles(string directory = "", string searchPattern = "*.*")
        {
            var diskFiles = (diskStorage != null && diskStorage.ExistsDirectory(directory))
                ? diskStorage.GetFiles(directory, searchPattern).Select(f => Path.GetFileName(f))
                : Enumerable.Empty<string>();

            var resourceFiles = Enumerable.Empty<string>();

            if (resourceAssembly != null)
            {
                var normalizedDir = directory.Replace('/', '.').Replace('\\', '.');
                if (!string.IsNullOrEmpty(normalizedDir) && !normalizedDir.EndsWith('.'))
                    normalizedDir += ".";

                var searchExt = searchPattern == "*.*" ? "" : searchPattern.Replace("*", "");

                resourceFiles = resourceAssembly.GetManifestResourceNames()
                    .Where(n => string.IsNullOrEmpty(normalizedDir) || n.Contains("." + normalizedDir, StringComparison.Ordinal) || n.StartsWith(normalizedDir, StringComparison.Ordinal))
                    .Where(n => string.IsNullOrEmpty(searchExt) || n.EndsWith(searchExt, StringComparison.OrdinalIgnoreCase))
                    .Select(n =>
                    {
                        var lastDot = n.LastIndexOf('.');
                        var extensionDot = n.LastIndexOf('.', lastDot - 1);
                        if (extensionDot >= 0 && lastDot > extensionDot)
                            return n.Substring(extensionDot + 1);
                        return n;
                    });
            }

            return diskFiles.Concat(resourceFiles).Distinct();
        }

        private string? TryResolveResourceName(string path)
        {
            var normalizedPath = path.Replace('/', '.').Replace('\\', '.');
            return resourceAssembly?.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("." + normalizedPath, StringComparison.OrdinalIgnoreCase) || n.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));
        }
        public string? GetFullPath(string path)
        {
            return diskStorage?.GetFullPath(path);
        }
    }
}
