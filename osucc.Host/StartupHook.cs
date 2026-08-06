using osucc.Common;
using osucc.Core;
using osucc.Plugin;

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

        // Route the shared resolver's diagnostics into the osu-cc timing log.
        OsuCcTimingLog.Error = message => TimingLog.Error($"OsuCcDataRootResolver: {message}");

        // Apply the Sentry error-reporting preference before any osu code runs: SentryLogger
        // snapshots OSU_DISABLE_ERROR_REPORTING once at construction, and reading the persisted
        // preference here (straight from disk) guarantees we beat it on every build, without a
        // version-specific patch target.
        SentryPreference.ApplyBeforeSentryLogger();

        // Plugin payloads carry an AssemblyRef to the osucc version they were compiled against; the
        // deployed hook and its sibling blobs (osucc.Shared.dll) are resolved from this assembly's
        // own directory by HookAssemblyResolver's module initializer, which runs before this method.
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
