using osu.Framework.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Profile.Sections.Beatmaps;
using osucc.Client;
using osucc.Core;
using osucc.Localisation;
using osuTK;

namespace osucc.UI.Profile
{
    /// <summary>
    /// Full-width button below the "Favourites" header that enqueues downloads for every favourited
    /// beatmap set of the profile's current user (see <see cref="ClientProfileDownloads"/>). Disabled
    /// while the fetch chain is running.
    /// </summary>
    public partial class DownloadAllFavouritesButton : RoundedButton
    {
        private readonly PaginatedBeatmapContainer container;

        public DownloadAllFavouritesButton(PaginatedBeatmapContainer container)
        {
            this.container = container;

            RelativeSizeAxes = Axes.X;
            Height = 40;
            Anchor = Anchor.TopCentre;
            Origin = Anchor.TopCentre;
            Margin = new MarginPadding { Bottom = 10 };

            Text = DownloadStrings.ButtonDefault;
            BackgroundColour = OsuCcColours.Pink;
            TooltipText = DownloadStrings.ButtonTooltip;

            Action = () =>
            {
                if (!ClientProfileDownloads.DownloadAllFavourites(container, complete))
                    return;

                Enabled.Value = false;
                Text = DownloadStrings.ButtonFetching;
            };

            ClientProfileDownloads.Register(this);
        }

        private void complete()
        {
            // The profile section can be disposed while the fetch chain is running; touching
            // the text sprite of a disposed drawable would throw on the update thread.
            if (DrawableHelper.IsDisposed(this))
                return;

            Enabled.Value = true;
            Text = DownloadStrings.ButtonDefault;
        }
    }
}
