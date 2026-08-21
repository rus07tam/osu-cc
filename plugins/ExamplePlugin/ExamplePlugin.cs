using osu.Framework.Bindables;
using osucc.Client;
using osucc.Core;
using osucc.Plugin;

namespace ExamplePlugin
{
    /// <summary>
    /// Reference implementation of the osu!cc plugin API: registers a toolbar button, a
    /// settings subsection (persisted via <see cref="PluginSettings"/>), shows notifications
    /// and celebrations, installs a Harmony patch by name, and walks through the optional
    /// lifecycle hooks (<see cref="IPluginLifecycle"/>) and data migrations
    /// (<see cref="IPluginMigrations"/>) via <see cref="OsuCcPlugin"/>. Also demonstrates
    /// plugin-to-plugin dependencies: it declares a dependency on the built-in
    /// <c>username-visuals</c> plugin (which exports <c>IUsernameVisualsApi</c>) and consumes
    /// that API via <c>Host.GetApi&lt;IUsernameVisualsApi&gt;</c>. Because the dependency is
    /// soft, a missing/disabled exporting plugin is handled by the consumer's null-check.
    /// </summary>
    public class ExamplePlugin : OsuCcPlugin
    {
        private const string PluginVersion = "1.0.0";

        private PluginSettings settings = null!;

        // The bindable returned by PluginSettings is the live config instance; hold it locally
        // so the value we read in AttachToGame reflects what the settings UI wrote.
        private Bindable<bool> celebrateToggle = null!;

        // Keeps the username-visuals API consumer (and its rule registrations) alive for the
        // plugin's lifetime; the consumer is created in AttachToGame, when the export exists.
        private ExampleUsernameVisualsApiConsumer? usernameVisualsConsumer;

        public override IReadOnlyList<OsuCcPatch> Patches => new OsuCcPatch[]
        {
            new ExampleHarmonyPatch(this, Host),
        };

        protected override void OnLoad()
        {
            // Defaults can be registered before the game exists; persisted values are loaded
            // from disk on AttachToGame.
            settings = Host.GetSettings();
            celebrateToggle = settings.Bind("celebrate", true);

            // Factories are invoked later, on the update thread (when the toolbar / settings
            // overlay is built), so constructing osu drawables here is safe.
            Host.AddToolbarButton(() => new ExampleToolbarButton(celebrateToggle, Host.Notify));
            Host.AddSettingsSubsection(() => new ExampleSettingsSubsection(settings, Host));

            // The patches declared in Patches are applied via InstallPatches().
            if (InstallPatches() > 0)
                Host.Log("Harmony patch installed");

            Host.Log("loaded");
        }

        public override void AttachToGame()
        {
            // Persisted config is now available; the value reflects what the user chose.
            Host.Log($"attach: celebrate = {celebrateToggle.Value}");

            Host.Notify(ExamplePluginStrings.Attached, NotificationKind.Success);

            // Consume the username-visuals API (see ExampleUsernameVisualsApiConsumer). The
            // consumer shares the plugin settings so the "Username Visuals integration" toggle
            // registers/revokes its demo rules live.
            try
            {
                usernameVisualsConsumer = new ExampleUsernameVisualsApiConsumer(Host, settings);
            }
            catch (Exception ex)
            {
                Host.Log($"username-visuals API demo skipped: {ex.Message}");
            }
        }

        public override void OnInstall()
        {
            Host.Log("installed");
            Host.Notify(ExamplePluginStrings.Installed, NotificationKind.Success);
        }

        public override void OnUpdate(string previousVersion)
        {
            Host.Log($"updated from {previousVersion} to {PluginVersion}");
            Host.Notify(ExamplePluginStrings.Updated(previousVersion, PluginVersion), NotificationKind.Info);
        }

        public override void OnUninstall()
        {
            Host.Log("uninstalled");
            Host.Notify(ExamplePluginStrings.Uninstalled, NotificationKind.Info);
        }

        public override int SchemaVersion => 2;

        public override IEnumerable<IPluginMigration> Migrations => new IPluginMigration[] { new RenameCelebrateSettingMigration() };

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            usernameVisualsConsumer?.Dispose();
            settings?.Dispose();
            base.Dispose();
        }

        /// <summary>Demo migration: schema v2 renames the persisted setting key <c>celebrate</c> to <c>celebration</c>.</summary>
        private sealed class RenameCelebrateSettingMigration : IPluginMigration
        {
            public int ToVersion => 2;

            public void Apply(PluginSettings settings, Action<string> log)
            {
                string? persisted = settings.ReadPersisted("celebrate");
                bool celebrate = persisted == null || bool.TryParse(persisted, out bool parsed) && parsed;

                settings.Bind("celebrate", celebrate).Value = celebrate;
                settings.Remove("celebrate");

                log("schema v2: renamed setting 'celebrate' -> 'celebration'");
            }
        }
    }
}
