using osu.Framework.Allocation;
using osu.Framework.Graphics;

namespace osucc.Client
{
    /// <summary>
    /// Invisible component added at startup. Once the game (and the dialog overlay) are loaded,
    /// its scheduler fires <see cref="ClientApi.MaybeShowFirstRunDisclaimer"/> on the update
    /// thread, retrying until the dialog overlay becomes available.
    /// </summary>
    public partial class FirstRunSetupComponent : Drawable
    {
        private const int initialDelayMs = 3000;
        private const int retryDelayMs = 2000;
        private const int maxAttempts = 15;

        private int attempts;

        [BackgroundDependencyLoader]
        private void load()
        {
            // Fire after the startup toast so the disclaimer never races it.
            Scheduler.AddDelayed(check, initialDelayMs, false);
        }

        private void check()
        {
            if (ClientApi.MaybeShowFirstRunDisclaimer())
                return;

            if (++attempts < maxAttempts)
                Scheduler.AddDelayed(check, retryDelayMs, false);
        }
    }
}
