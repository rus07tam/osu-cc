using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game;
using osucc.Core;
using osucc.Localisation;
using osucc.Plugin;
using osucc.UI.Plugins;

namespace osucc.Client
{
    /// <summary>
    /// The client's public surface: holds the live game instance and config, and bridges the
    /// (reflective) patches with the (typed) UI/API layers.
    /// </summary>
    public static class ClientApi
    {
        public const string BrandingName = "osu!cc";

        public static OsuGameBase? Game { get; private set; }

        public static SpecialsConfigManager? Config { get; private set; }

        // ConfigManager.GetBindable returns weak copies; hold strong references or the
        // BindValueChanged subscriptions die after the first (immediate) fire.
        private static Bindable<bool>? brandingBindable;
        private static Bindable<bool>? showSystemModsBindable;
        private static Bindable<bool>? firstRunSetupCompleteBindable;

        public static string? OriginalGameName { get; private set; }

        /// <summary>Remembers the game's real name before branding overwrites it.</summary>
        public static void CaptureOriginalGameName(string? name) => OriginalGameName ??= name;

        /// <summary>
        /// Called from the <c>OsuGameBase.load</c> postfix, once the instance, storage and
        /// dependency injection are available. Wires up config, branding and the startup
        /// notification component.
        /// </summary>
        public static void AttachToGame(OsuGameBase game)
        {
            if (Game != null)
                return;

            Game = game;
            TimingLog.Info($"ClientApi attached to {game.GetType().FullName}");

            var storage = Reflection.GetStorage(game);

            if (storage == null)
            {
                ClientState.AddError("game Storage not available");
                return;
            }

            Config = new SpecialsConfigManager(storage.GetStorageForDirectory("osu-cc"));
            Config.Load();
            TimingLog.Info($"SpecialsConfigManager loaded (branding default: {Config.GetBindable<bool>(SpecialsSetting.Branding).Value})");

            applySentryReportingPreference();

            ClientSupporter.Attach(Config);
            ClientFavourites.Attach(Config);
            ClientProfileDownloads.Attach(Config);

            // Live-binding: toggling the checkbox in the Specials section flips the window title.
            brandingBindable = Config.GetBindable<bool>(SpecialsSetting.Branding);
            brandingBindable.BindValueChanged(v => applyBranding(v.NewValue), true);

            // Live-binding: toggling "Show System mods" adds/removes the column on open overlays immediately.
            showSystemModsBindable = Config.GetBindable<bool>(SpecialsSetting.ShowSystemMods);
            showSystemModsBindable.BindValueChanged(_ => ClientMods.RefreshOverlays(), true);

            firstRunSetupCompleteBindable = Config.GetBindable<bool>(SpecialsSetting.FirstRunSetupComplete);

            game.Add(new InitNotificationsComponent());
            game.Add(new FirstRunSetupComponent());
            game.Add(new PluginsOverlayComponent());

            // Storage and DI are available: attach every loaded plugin (its settings reload
            // from disk first, then AttachToGame runs on the update thread).
            PluginManager.AttachAllToGame();
        }

        private static void applyBranding(bool enabled)
        {
            if (Game == null)
                return;

            Reflection.SetName(Game, enabled ? BrandingName : OriginalGameName ?? BrandingName);
            TimingLog.Info($"Branding applied: enabled={enabled}");
        }

        /// <summary>
        /// Applies the <see cref="SpecialsSetting.SentryErrorReporting"/> preference via osu's own
        /// kill-switch env var (<c>OSU_DISABLE_ERROR_REPORTING</c>). Must run before
        /// <c>OsuGame.load</c> constructs <c>SentryLogger</c>; since the logger reads the env var
        /// once at construction, the preference takes effect on the next launch.
        /// </summary>
        private static void applySentryReportingPreference()
        {
            bool enabled = Config?.GetBindable<bool>(SpecialsSetting.SentryErrorReporting).Value ?? false;

            if (enabled)
            {
                // Removing the variable (null) re-enables the game's default behaviour.
                Environment.SetEnvironmentVariable("OSU_DISABLE_ERROR_REPORTING", null);
                TimingLog.Info("Sentry error reporting ENABLED (OSU_DISABLE_ERROR_REPORTING cleared)");
            }
            else
            {
                Environment.SetEnvironmentVariable("OSU_DISABLE_ERROR_REPORTING", "1");
                TimingLog.Info("Sentry error reporting DISABLED (OSU_DISABLE_ERROR_REPORTING=1)");
            }
        }

        /// <summary>Posted by the scheduler once startup is complete.</summary>
        public static void ReportInit()
        {
            TimingLog.Info("ReportInit fired");

            if (ClientState.IsFaulted)
                ClientNotifications.Error(OsuCcStrings.InitializationFailed(string.Join("; ", ClientState.Errors)));
            else
            {
                ClientState.MarkReady();
                ClientNotifications.Success(OsuCcStrings.ClientLoaded(BrandingName));
            }

            reportPluginLoadSummary();
        }

        /// <summary>Surfaces how many plugins loaded versus failed, so a broken plugin is visible without opening the plugins overlay.</summary>
        private static void reportPluginLoadSummary()
        {
            var (loaded, failed) = PluginManager.GetLoadSummary();

            int total = loaded + failed;
            if (total == 0)
            {
                TimingLog.Info("Plugin load summary skipped (no plugins discovered)");
                return;
            }

            LocalisableString summary = failed > 0
                ? OsuCcStrings.PluginsLoadedFailed(loaded, total, failed)
                : OsuCcStrings.PluginsLoaded(loaded, total);

            if (failed > 0)
                ClientNotifications.Error(summary);
            else
                ClientNotifications.Info(summary);
        }

        /// <summary>
        /// Shows the first-run disclaimer dialog once, unless already acknowledged. Called from
        /// the <see cref="FirstRunSetupComponent"/> scheduler once the game has settled.
        /// </summary>
        /// <returns><c>false</c> if the dialog overlay is not yet available and the caller should retry.</returns>
        public static bool MaybeShowFirstRunDisclaimer()
        {
            if (Game == null || Config == null)
                return false;

            if (Config.GetBindable<bool>(SpecialsSetting.FirstRunSetupComplete).Value)
            {
                TimingLog.Info("First-run disclaimer skipped (already completed)");
                return true;
            }

            var overlay = Reflection.GetDialogOverlay(Game);

            if (overlay == null)
            {
                TimingLog.Info("First-run disclaimer: DialogOverlay not available yet; will retry");
                return false;
            }

            overlay.Push(new OsuCcDisclaimerDialog(() =>
            {
                Config.GetBindable<bool>(SpecialsSetting.FirstRunSetupComplete).Value = true;
                TimingLog.Info("First-run disclaimer acknowledged; FirstRunSetupComplete set to true");
            }));

            TimingLog.Info("First-run disclaimer shown");
            return true;
        }
    }
}
