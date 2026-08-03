using osucc.Client;
using osucc.Core;

namespace osucc.Celebrations
{
    /// <summary>
    /// Public API for showing full-screen <see cref="Celebration"/> overlays. Handles posting onto
    /// the update thread and adding into the game's top-most overlay container.
    /// </summary>
    public static class ClientCelebrations
    {
        /// <summary>
        /// Shows a celebration over the game's top-most overlay content. Safe to call from any
        /// thread; the drawable is added on the update thread.
        /// </summary>
        public static void Show(Celebration celebration)
        {
            var game = ClientApi.Game;

            if (game == null)
            {
                TimingLog.Error($"Celebrations: no game instance available; \"{celebration.GetType().Name}\" dropped");
                return;
            }

            Reflection.GetScheduler(game)?.Add(() =>
            {
                var container = Reflection.GetTopMostOverlayContent(game);
                if (container == null)
                {
                    TimingLog.Error("Celebrations: top-most overlay container unavailable; celebration dropped");
                    return;
                }

                container.Add(celebration);
                TimingLog.Info($"Celebration shown: {celebration.GetType().Name}");
            });
        }
    }
}
