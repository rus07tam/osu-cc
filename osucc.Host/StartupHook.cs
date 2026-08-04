using osucc.Core;
using osucc.Plugin;
using System.Runtime.Loader;

/// <summary>
/// .NET startup hook: <see cref="Initialize"/> runs before <c>Main()</c> and before
/// <c>osu.Game.dll</c> is loaded. It subscribes to <see cref="AppDomain.AssemblyLoad"/>
/// and installs the patches the moment the target assemblies appear.
/// The runtime resolves this type via <c>Assembly.GetType("StartupHook")</c>, so the
/// class must keep this exact name in the global namespace.
/// </summary>
#pragma warning disable CA1050 // The startup-hook contract requires this type in the global namespace.
public class StartupHook
{
    private static bool installed;

    private static readonly object lockObject = new();

    /// <summary>DOTNET_STARTUP_HOOKS entry point.</summary>
    public static void Initialize()
    {
        TimingLog.Info("Initialize() called");

        // Apply the Sentry error-reporting preference before any osu code runs: SentryLogger
        // snapshots OSU_DISABLE_ERROR_REPORTING once at construction, and reading the persisted
        // preference here (straight from disk) guarantees we beat it on every build, without a
        // version-specific patch target.
        SentryPreference.ApplyBeforeSentryLogger();

        // Plugin payloads carry an AssemblyRef to the osucc version they were compiled against.
        // That version can lag the deployed hook (e.g. a stale archive from before a version bump),
        // and the default ALC binds by exact version, which would silently drop every plugin type.
        // Bind any requested 'osucc' to the already-loaded hook assembly instead, so a payload
        // referencing any osucc version loads against the deployed hook.
        AssemblyLoadContext.Default.Resolving += (_, name) =>
            name.Name == "osucc" ? typeof(StartupHook).Assembly : null;

        AppDomain.CurrentDomain.AssemblyLoad += (_, args) =>
        {
            try
            {
                if (args.LoadedAssembly.GetName().Name == "osu.Game")
                    tryInstall();
            }
            catch (Exception ex)
            {
                TimingLog.Error($"AssemblyLoad handler: {ex}");
            }
        };

        // In case osu.Game.dll was already loaded before we subscribed.
        tryInstall();
    }

    private static void tryInstall()
    {
        if (installed)
            return;

        var osuGame = AppDomain.CurrentDomain.GetAssemblies()
                               .FirstOrDefault(a => a.GetName().Name == "osu.Game");
        if (osuGame == null)
            return;

        lock (lockObject)
        {
            if (installed)
                return;

            TimingLog.Info($"osu.Game.dll loaded at {DateTime.Now:HH:mm:ss.fff}");
            HookDependencies.EnsureLoaded();
            ClientBootstrapper.InstallPatches();
            PluginManager.LoadAll();
            installed = true;
        }
    }
}
#pragma warning restore CA1050
