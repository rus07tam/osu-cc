using osu.Framework.Localisation;
using osu.Game;
using osucc.Core;
using osucc.Localisation;
using osucc.Plugin;

namespace osucc.Client
{
    /// <summary>
    /// The client's public surface: holds the live game instance and config, and bridges the
    /// (reflective) patches with the (typed) UI/API layers. The one-time wiring lives in
    /// <see cref="ClientBootstrap"/>.
    /// </summary>
    public static class ClientApi
    {
        public const string BrandingName = "osu!cc";

        public static OsuGameBase? Game { get; private set; }

        public static SpecialsConfigManager? Config { get; private set; }

        public static string? OriginalGameName { get; private set; }

        /// <summary>Remembers the game's real name before branding overwrites it.</summary>
        public static void CaptureOriginalGameName(string? name) => OriginalGameName ??= name;

        /// <summary>Bound by <see cref="ClientBootstrap.AttachToGame"/>.</summary>
        internal static void SetGame(OsuGameBase game) => Game = game;

        /// <summary>Bound by <see cref="ClientBootstrap.AttachToGame"/>.</summary>
        internal static void SetConfig(SpecialsConfigManager config) => Config = config;

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

            if (ClientConfig.FirstRunSetupComplete.Value)
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
                ClientConfig.FirstRunSetupComplete.Value = true;
                TimingLog.Info("First-run disclaimer acknowledged; FirstRunSetupComplete set to true");
            }));

            TimingLog.Info("First-run disclaimer shown");
            return true;
        }
    }
}
