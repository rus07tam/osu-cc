using System.Reflection;

namespace osucc.Data
{
    /// <summary>
    /// Manages scoped storage instances.
    /// </summary>
    public interface IOsuCcStorageManager
    {
        /// <summary>
        /// Gets or creates a storage instance for the given scope.
        /// </summary>
        /// <param name="scope">The storage namespace (e.g., "core" or a plugin id).</param>
        /// <param name="resourceAssembly">An optional assembly to use for embedded resource fallback.</param>
        IOsuCcStorage GetStorage(string scope, Assembly? resourceAssembly = null);
    }
}
