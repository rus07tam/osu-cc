using osu.Game;
using osucc.Core;
using osucc.Patches;
using osucc.Plugin;
using osucc.UI.Overlays;
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

            ClientApi.SetStorageManager(new osucc.Data.OsuCcStorageManager(storage));

            var config = new SpecialsConfigManager(storage.GetStorageForDirectory("osu-cc"));
            ClientApi.SetConfig(config);
            config.Load();
            ClientConfig.Attach(config);
            TimingLog.Info($"SpecialsConfigManager loaded (branding default: {ClientConfig.Branding.Value})");

            // Cosmetic UI theme: pin the persisted id (falling back to the vanilla default if it no
            // longer resolves) before any client overlay is built, then re-paint the game's
            // OsuColour accents and osu-cc's own OsuCcColours (the OverlayColourProvider patch
            // reads the active theme dynamically, so it needs no setup). Restart-gated in the
            // Specials settings section.
            if (!OsuCcThemeRegistry.TryGet(ClientConfig.OsuCcTheme.Value, out var theme))
                theme = OsuCcThemeRegistry.Get(OsuCcThemeRegistry.DefaultId);
            OsuCcThemeManager.SetActive(theme);
            OsuCcThemeManager.ApplyToGame(game);

            TimingLog.Info($"OsuCc theme active: {OsuCcThemeManager.Active.Id}");

            ClientFavourites.Attach();
            ClientProfileDownloads.Attach();

            // Live-binding: toggling the checkbox in the Specials section flips the window title.
            ClientConfig.Branding.BindValueChanged(v => applyBranding(v.NewValue), true);

            // Live-binding: toggling "Show System mods" adds/removes the column on open overlays immediately.
            ClientConfig.ShowSystemMods.BindValueChanged(_ => ClientMods.RefreshOverlays(), true);

            game.Add(new InitNotificationsComponent());
            game.Add(new FirstRunSetupComponent());
            game.Add(new PluginsOverlayComponent());
            game.Add(new ThemePreviewComponent());
            game.Add(new KeyHistoryOverlayComponent());

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
