using osu.Game;
using osucc.Core;
using osucc.Plugin;
using osucc.UI.Plugins;

namespace osucc.Client
{
    /// <summary>
    /// Owns the one-time wiring between the (reflective) patches and the typed client layers:
    /// resolves storage, builds the <see cref="SpecialsConfigManager"/>, attaches each client
    /// subsystem, sets up the live config bindables and the startup components, then attaches
    /// every plugin. Kept separate from <see cref="ClientApi"/> so that class stays a thin
    /// public surface (game instance, config, branding, startup toasts).
    /// </summary>
    public static class ClientBootstrap
    {
        /// <summary>
        /// Called from the <c>OsuGameBase.load</c> postfix, once the instance, storage and
        /// dependency injection are available. Wires up config, branding and the startup
        /// notification component. Idempotent.
        /// </summary>
        public static void AttachToGame(OsuGameBase game)
        {
            if (ClientApi.Game != null)
                return;

            ClientApi.SetGame(game);
            TimingLog.Info($"ClientBootstrap attached to {game.GetType().FullName}");

            var storage = Reflection.GetStorage(game);

            if (storage == null)
            {
                ClientState.AddError("game Storage not available");
                return;
            }

            var config = new SpecialsConfigManager(storage.GetStorageForDirectory("osu-cc"));
            ClientApi.SetConfig(config);
            config.Load();
            ClientConfig.Attach(config);
            TimingLog.Info($"SpecialsConfigManager loaded (branding default: {ClientConfig.Branding.Value})");

            ClientSupporter.Attach();
            ClientFavourites.Attach();
            ClientProfileDownloads.Attach();

            // Live-binding: toggling the checkbox in the Specials section flips the window title.
            ClientConfig.Branding.BindValueChanged(v => applyBranding(v.NewValue), true);

            // Live-binding: toggling "Show System mods" adds/removes the column on open overlays immediately.
            ClientConfig.ShowSystemMods.BindValueChanged(_ => ClientMods.RefreshOverlays(), true);

            game.Add(new InitNotificationsComponent());
            game.Add(new FirstRunSetupComponent());
            game.Add(new PluginsOverlayComponent());

            // Storage and DI are available: attach every loaded plugin (its settings reload
            // from disk first, then AttachToGame runs on the update thread).
            PluginManager.AttachAllToGame();
        }

        private static void applyBranding(bool enabled)
        {
            if (ClientApi.Game == null)
                return;

            Reflection.SetName(ClientApi.Game, enabled ? ClientApi.BrandingName : ClientApi.OriginalGameName ?? ClientApi.BrandingName);
            TimingLog.Info($"Branding applied: enabled={enabled}");
        }
    }
}