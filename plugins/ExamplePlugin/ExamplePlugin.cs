using osu.Framework.Bindables;
using osucc.Client;
using osucc.Plugin;

namespace ExamplePlugin
{
    /// <summary>
    /// Reference implementation of the osu!cc plugin API: registers a toolbar button, a
    /// settings subsection (persisted via <see cref="PluginSettings"/>), shows notifications
    /// and celebrations, installs a Harmony patch by name, and walks through the optional
    /// lifecycle hooks (<see cref="IPluginLifecycle"/>) and data migrations
    /// (<see cref="IPluginMigrations"/>) via <see cref="OsuCcPluginBase"/>.
    /// </summary>
    [OsuCcPlugin(
        "example",
        "Example Plugin",
        100,
        Author = "osu-cc",
        Description = "Demonstrates the osu!cc plugin API: toolbar button, notifications, celebrations, settings, a Harmony patch, lifecycle hooks and a data migration.",
        Version = "1.0.0")]
    public class ExamplePlugin : OsuCcPluginBase
    {
        private const string PluginVersion = "1.0.0";

        private PluginSettings settings = null!;

        // The bindable returned by PluginSettings is the live config instance; hold it locally
        // so the value we read in AttachToGame reflects what the settings UI wrote.
        private Bindable<bool> celebrateToggle = null!;

        protected override void OnLoad()
        {
            // Defaults can be registered before the game exists; persisted values are loaded
            // from disk on AttachToGame.
            settings = Host.GetSettings();
            celebrateToggle = settings.Bind("celebration", true);

            // Factories are invoked later, on the update thread (when the toolbar / settings
            // overlay is built), so constructing osu drawables here is safe.
            Host.AddToolbarButton(() => new ExampleToolbarButton(celebrateToggle, Host.Notify));
            Host.AddSettingsSubsection(() => new ExampleSettingsSubsection(settings));

            // A Harmony patch resolved by name against the runtime osu.Game assembly.
            if (ExampleHarmonyPatch.Install(Host))
                Host.Log("Harmony patch installed");

            Host.Log("loaded");
        }

        public override void AttachToGame()
        {
            // Persisted config is now available; the value reflects what the user chose.
            Host.Log($"attach: celebrate = {celebrateToggle.Value}");

            Host.Notify(ExamplePluginStrings.Attached, ClientNotifications.NotificationKind.Success);
        }

        public override void OnInstall(IOsuCcPluginHost host)
        {
            host.Log("installed");
            host.Notify(ExamplePluginStrings.Installed, ClientNotifications.NotificationKind.Success);
        }

        public override void OnUpdate(IOsuCcPluginHost host, string previousVersion)
        {
            host.Log($"updated from {previousVersion} to {PluginVersion}");
            host.Notify(ExamplePluginStrings.Updated(previousVersion, PluginVersion), ClientNotifications.NotificationKind.Info);
        }

        public override void OnUninstall(IOsuCcPluginHost host)
        {
            host.Log("uninstalled");
            host.Notify(ExamplePluginStrings.Uninstalled, ClientNotifications.NotificationKind.Info);
        }

        public override int SchemaVersion => 2;

        public override IEnumerable<IPluginMigration> Migrations => new IPluginMigration[] { new RenameCelebrateSettingMigration() };

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            settings?.Dispose();
            base.Dispose();
        }

        /// <summary>Demo migration: schema v2 renames the persisted setting key <c>celebrate</c> to <c>celebration</c>.</summary>
        private sealed class RenameCelebrateSettingMigration : IPluginMigration
        {
            public int ToVersion => 2;

            public void Apply(IOsuCcPluginHost host)
            {
                var settings = host.GetSettings();

                string? persisted = settings.ReadPersisted("celebrate");
                bool celebrate = persisted == null || bool.TryParse(persisted, out bool parsed) && parsed;

                settings.Bind("celebration", celebrate).Value = celebrate;
                settings.Remove("celebrate");

                host.Log("schema v2: renamed setting 'celebrate' -> 'celebration'");
            }
        }
    }
}
