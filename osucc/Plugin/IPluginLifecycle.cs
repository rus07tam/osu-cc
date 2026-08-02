namespace osucc.Plugin
{
    /// <summary>
    /// Optional lifecycle hooks a plugin can implement to react to install / uninstall / update
    /// events. All hooks run on the update thread once the game instance is available, so
    /// <see cref="IOsuCcPluginHost.GetSettings"/> and <see cref="IOsuCcPluginHost.GetStorage"/> are usable.
    /// </summary>
    public interface IPluginLifecycle
    {
        /// <summary>
        /// Called once, on the first launch after the plugin was installed — after
        /// <see cref="IOsuCcPlugin.AttachToGame"/> succeeded. Seed plugin data here.
        /// </summary>
        void OnInstall(IOsuCcPluginHost host)
        {
        }

        /// <summary>
        /// Called when the user confirms deletion, while the plugin is still loaded and the game
        /// runs. The payload folder is removed on the next launch; use this to release anything
        /// outside it (revoke tokens, flush remote state, …). Exceptions are logged and ignored.
        /// </summary>
        void OnUninstall(IOsuCcPluginHost host)
        {
        }

        /// <summary>
        /// Called when the loaded plugin version differs from the last recorded one — after data
        /// migrations (see <see cref="IPluginMigrations"/>) and after <see cref="IOsuCcPlugin.AttachToGame"/>.
        /// </summary>
        /// <param name="previousVersion">The version recorded on the last launch, in its raw string form.</param>
        void OnUpdate(IOsuCcPluginHost host, string previousVersion)
        {
        }
    }
}
