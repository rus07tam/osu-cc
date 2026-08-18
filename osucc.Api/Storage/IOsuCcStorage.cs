using System.Collections.Generic;
using System.IO;

namespace osucc.Data
{
    /// <summary>
    /// Represents a scoped storage area for resources and configuration (e.g., a specific plugin or the core).
    /// Provides a unified API over physical disk files and embedded assembly resources.
    /// </summary>
    public interface IOsuCcStorage
    {
        /// <summary>The namespace this storage is bound to (e.g., "core" or a plugin's id).</summary>
        string Scope { get; }

        /// <summary>
        /// Reads and deserializes a JSON file from the storage. Falls back to embedded resources if not found on disk.
        /// </summary>
        T? ReadJson<T>(string path, T? defaultValue = null) where T : class;

        /// <summary>
        /// Serializes and writes data as JSON to the physical disk storage.
        /// </summary>
        void WriteJson<T>(string path, T data) where T : class;

        /// <summary>
        /// Opens a stream to a file. For Read access, falls back to embedded resources if not found on disk.
        /// </summary>
        Stream? GetStream(string path, FileAccess access = FileAccess.Read);

        /// <summary>Checks if a file exists on disk or in embedded resources.</summary>
        bool Exists(string path);

        /// <summary>Gets a list of all files in the given directory (combines disk and embedded resources).</summary>
        IEnumerable<string> GetFiles(string directory = "", string searchPattern = "*.*");
        string? GetFullPath(string path);
    }
}
