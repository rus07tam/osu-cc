using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osucc.Common.Update;

namespace osucc.Launcher.UI;

public partial class OperationProgressBar : VisibilityContainer
{
    private ProgressBar progressBar = null!;
    private OsuSpriteText stageText = null!;

    [Resolved]
    private OverlayColourProvider colourProvider { get; set; } = null!;

    public OperationProgressBar()
    {
        RelativeSizeAxes = Axes.X;
        Height = 36;
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        // dark background + progress bar + stage text
        InternalChildren = new Drawable[]
        {
            new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = colourProvider.Background5,
                Alpha = 0.95f,
            },
            progressBar = new ProgressBar(false)
            {
                RelativeSizeAxes = Axes.X,
                Height = 3,
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft,
            },
            stageText = new OsuSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Font = OsuFont.Default.With(size: 13),
            },
        };
    }

    protected override void PopIn() => this.FadeIn(200, Easing.OutQuint);
    protected override void PopOut() => this.FadeOut(200, Easing.OutQuint);

    public void SetProgress(UpdateStage stage, float progress)
    {
        progressBar.Current.Value = progress;
        stageText.Text = stage switch
        {
            UpdateStage.Checking => "Checking for updates...",
            UpdateStage.Downloading => "Downloading...",
            UpdateStage.Extracting => "Extracting...",
            UpdateStage.Applying => "Applying update...",
            UpdateStage.Done => "Done!",
            UpdateStage.Failed => "Update failed",
            _ => string.Empty,
        };

        if (stage is UpdateStage.Done or UpdateStage.Failed)
            Scheduler.AddDelayed(Hide, 2000);
        else if (State.Value != Visibility.Visible)
            Show();
    }
}
