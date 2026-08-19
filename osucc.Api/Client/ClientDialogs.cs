using osu.Framework.Localisation;
using osu.Game.Overlays.Dialog;
using osucc.Core;
using System;

namespace osucc.Client
{
    /// <summary>
    /// Public API for showing dialogs through the game's own <see cref="osu.Game.Overlays.DialogOverlay"/>.
    /// Resolves the overlay reflectively from the live game instance and marshals the push onto the
    /// update thread, so it is safe to call from any thread (including background Harmony patches).
    /// </summary>
    public static class ClientDialogs
    {
        /// <summary>Shows a destructive-action confirmation (trash icon, hold-to-confirm button).</summary>
        /// <returns><c>false</c> when the game or dialog overlay is unavailable and the dialog was not queued.</returns>
        public static bool Confirm(LocalisableString title, LocalisableString body, Action confirmed)
            => Push(new OsuCcConfirmDialog(title, body, confirmed));

        /// <summary>Shows a non-destructive confirm for actions that need a restart (e.g. changing the UI theme).</summary>
        /// <returns><c>false</c> when the game or dialog overlay is unavailable and the dialog was not queued.</returns>
        public static bool Restart(LocalisableString title, LocalisableString body, LocalisableString confirmText, Action confirmed)
            => Push(new OsuCcRestartDialog(title, body, confirmText, confirmed));

        /// <summary>
        /// Queues an arbitrary <see cref="PopupDialog"/> onto the game's dialog overlay. The push is
        /// performed on the update thread, so the call itself is thread-safe.
        /// </summary>
        /// <returns><c>false</c> when the game, the dialog overlay or the scheduler is unavailable and the dialog was not queued.</returns>
        public static bool Push(PopupDialog dialog)
        {
            var game = ClientApi.Game;

            if (game == null)
            {
                TimingLog.Error($"Dialogs: no game instance available; \"{dialog.GetType().Name}\" dropped");
                return false;
            }

            var overlay = Reflection.GetDialogOverlay(game);

            if (overlay == null)
            {
                TimingLog.Error("Dialogs: dialog overlay unavailable; dialog dropped");
                return false;
            }

            var scheduler = Reflection.GetScheduler(game);

            if (scheduler == null)
            {
                TimingLog.Error("Dialogs: scheduler unavailable; dialog dropped");
                return false;
            }

            scheduler.Add(() => overlay.Push(dialog));
            TimingLog.Info($"Dialog queued: {dialog.GetType().Name}");
            return true;
        }
    }
}
