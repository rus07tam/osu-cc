using osu.Framework.Allocation;
using osu.Framework.Graphics;

namespace osucc.Client
{
    /// <summary>
    /// Invisible component added at startup. When the game finishes loading, its scheduler fires
    /// <see cref="ClientApi.ReportInit"/> on the update thread, so the startup toast posts last.
    /// </summary>
    public partial class InitNotificationsComponent : Drawable
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Scheduler.AddDelayed(ClientApi.ReportInit, 2000, false);
        }
    }
}
