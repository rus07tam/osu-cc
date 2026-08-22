using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Screens;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osucc.Common.Update;
using osucc.Launcher.UI;
using osuTK;
using System;
using System.Threading.Tasks;

namespace osucc.Launcher.Screens;

public partial class MainScreen : Screen
{
    [Resolved]
    private OverlayColourProvider colourProvider { get; set; } = null!;

    [Resolved]
    private OsuCcUpdateService updateService { get; set; } = null!;

    private ShearedButton updateButton = null!;
    private ShearedButton reinstallButton = null!;
    private ShearedButton installButton = null!;
    private ShearedButton uninstallButton = null!;

    private readonly Bindable<bool> operationInProgress = new(false);
    private OperationProgressBar progressBar = null!;

    [BackgroundDependencyLoader]
    private void load()
    {
        // Build UI: title + buttons + progressBar at bottom
        InternalChildren = new Drawable[]
        {
            new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Direction = FillDirection.Vertical,
                        AutoSizeAxes = Axes.Both,
                        Spacing = new Vector2(0, 2),
                        Margin = new MarginPadding { Bottom = 20 },
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Text = "osu!cc",
                                Font = OsuFont.GetFont(size: 40, weight: FontWeight.Bold)
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Text = "custom client for osu!lazer",
                                Colour = colourProvider.Content2,
                                Font = OsuFont.GetFont(size: 16, weight: FontWeight.Regular)
                            }
                        }
                    },
                    updateButton = new ShearedButton
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Width = 300,
                        Text = "Update osu!cc",
                        Action = runUpdate,
                    },
                    reinstallButton = new ShearedButton
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Width = 300,
                        Text = "Reinstall osu!cc",
                        Action = runInstall,
                    },
                    installButton = new ShearedButton
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Width = 300,
                        Text = "Install osu!cc",
                        Action = runInstall,
                    },
                    uninstallButton = new ShearedButton
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Width = 300,
                        DarkerColour = colourProvider.Colour4,
                        LighterColour = colourProvider.Colour3,
                        Text = "Uninstall osu!cc",
                        Action = runUninstall,
                    },
                }
            },
            progressBar = new OperationProgressBar
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                State = { Value = Visibility.Hidden },
            },
        };
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();
        operationInProgress.BindValueChanged(e => updateButtonStates(), true);
        updateButtonVisibility();
    }

    private void updateButtonVisibility()
    {
        bool installed = updateService.IsInstalled;
        installButton.Alpha = installed ? 0 : 1;
        updateButton.Alpha = installed ? 1 : 0;
        reinstallButton.Alpha = installed ? 1 : 0;
        uninstallButton.Alpha = installed ? 1 : 0;
    }

    private void updateButtonStates()
    {
        bool busy = operationInProgress.Value;
        updateButton.Enabled.Value = !busy;
        reinstallButton.Enabled.Value = !busy;
        installButton.Enabled.Value = !busy;
        uninstallButton.Enabled.Value = !busy;
    }

    private void runUpdate()
    {
        operationInProgress.Value = true;
        var progress = new Progress<(UpdateStage, float, string?)>(t => Schedule(() => progressBar.SetProgress(t.Item1, t.Item2, t.Item3)));
        Task.Run(async () =>
        {
            try
            {
                await updateService.UpdateAsync(progress).ConfigureAwait(false);
            }
            finally
            {
                Schedule(() =>
                {
                    operationInProgress.Value = false;
                    updateButtonVisibility();
                });
            }
        });
    }

    private void runInstall()
    {
        operationInProgress.Value = true;
        var progress = new Progress<(UpdateStage, float, string?)>(t => Schedule(() => progressBar.SetProgress(t.Item1, t.Item2, t.Item3)));
        Task.Run(async () =>
        {
            try
            {
                await updateService.InstallAsync(progress).ConfigureAwait(false);
            }
            finally
            {
                Schedule(() =>
                {
                    operationInProgress.Value = false;
                    updateButtonVisibility();
                });
            }
        });
    }

    private void runUninstall()
    {
        operationInProgress.Value = true;
        Task.Run(async () =>
        {
            try
            {
                await updateService.UninstallAsync().ConfigureAwait(false);
            }
            finally
            {
                Schedule(() =>
                {
                    operationInProgress.Value = false;
                    updateButtonVisibility();
                });
            }
        });
    }
}
