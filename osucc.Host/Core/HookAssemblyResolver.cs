using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace osucc.Core;

/// <summary>
/// Bootstraps the startup hook's assembly resolution before <see cref="StartupHook.Initialize"/>
/// runs. The runtime loads the hook from the osu-cc data root's <c>hook/</c> folder via
/// <c>Assembly.LoadFrom</c>, but the default ALC probes only the game's app base for dependencies
/// — never the hook's own folder. So a sibling blob (e.g. <c>osucc.Shared.dll</c>) that the hook
/// references at JIT time is "missing" even though it sits right next to <c>osucc.dll</c>.
///
/// A module initializer runs the moment the hook assembly is loaded, i.e. before
/// <c>StartupHook.Initialize()</c> is JIT-compiled, so a <c>Resolving</c> handler installed here
/// is already in place when the hook's own types need their dependencies.
/// </summary>
#pragma warning disable CA2255 // The startup-hook contract requires resolution to be wired before Initialize() JITs.
internal static class HookAssemblyResolver
{
    [ModuleInitializer]
    internal static void InitializeResolver()
    {
        Assembly hookAssembly = typeof(StartupHook).Assembly;
        string? hookDirectory = Path.GetDirectoryName(hookAssembly.Location);

        if (hookDirectory == null)
            return;

        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            // Plugin payloads carry an AssemblyRef to the osucc version they were compiled against.
            // That version can lag the deployed hook (e.g. a stale archive from before a version
            // bump), and the default ALC binds by exact version, which would silently drop every
            // plugin type. Bind any requested 'osucc' to the already-loaded hook assembly instead,
            // so a payload referencing any osucc version loads against the deployed hook.
            if (name.Name == "osucc")
                return hookAssembly;

            // Any other hook-owned blob (osucc.Shared.dll, ...) lives next to the hook in the data
            // root's hook/ folder; the app never has it, so serve it from here.
            string candidate = Path.Combine(hookDirectory, name.Name + ".dll");
            return File.Exists(candidate) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate) : null;
        };
    }
}
