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
            if (osucc.Client.ClientApi.Game != null)
                return;

            ClientHostTasks.SetGame(game);

            osucc.Core.TimingLog.LogDirectoryProvider = () => osucc.Plugin.PluginDirectories.ResolveLogsDirectory();
            osucc.Core.TextureHelper.RendererProvider = () => game.Dependencies?.Get(typeof(osu.Framework.Graphics.Rendering.IRenderer)) as osu.Framework.Graphics.Rendering.IRenderer;

            TimingLog.Info($"ClientBootstrap attached to {game.GetType().FullName}");

            var storage = Reflection.GetStorage(game);

            if (storage == null)
            {
                ClientState.AddError("game Storage not available");
                return;
            }

            ClientHostTasks.SetStorageManager(new osucc.Data.OsuCcStorageManager(storage));

            var config = new SpecialsConfigManager(storage.GetStorageForDirectory("osu-cc"));
            ClientHostTasks.SetConfig(config);
            config.Load();
            ClientConfig.Attach(config);
            TimingLog.Info($"SpecialsConfigManager loaded (branding default: {ClientConfig.Branding.Value})");

            if (!OsuCcThemeRegistry.TryGet(ClientConfig.OsuCcTheme.Value, out var theme))
                theme = OsuCcThemeRegistry.Get(OsuCcThemeRegistry.DefaultId);
            OsuCcThemeManager.SetActive(theme);
            OsuCcThemeManager.ApplyToGame(game);

            TimingLog.Info($"OsuCc theme active: {OsuCcThemeManager.Active.Id}");

            ClientFavourites.Attach();
            ClientProfileDownloads.Attach();

            ClientConfig.Branding.BindValueChanged(v => applyBranding(v.NewValue), true);
            ClientConfig.ShowSystemMods.BindValueChanged(_ => ClientMods.RefreshOverlays(), true);

            game.Add(new InitNotificationsComponent());
            game.Add(new FirstRunSetupComponent());
            game.Add(new PluginsOverlayComponent());
            game.Add(new ThemePreviewComponent());
            game.Add(new KeyHistoryOverlayComponent());

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
