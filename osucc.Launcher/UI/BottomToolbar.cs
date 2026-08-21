using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osucc.Launcher.Core;
using osuTK;
using osuTK.Graphics;
using System;
using System.Diagnostics;

namespace osucc.Launcher.UI
{
    public partial class BottomToolbar : Container
    {
        public Action<int>? OnTabSelected { get; set; }
        private Process? runningGameProcess;

        private Box playButtonBackground = null!;
        private SpriteIcon playButtonIcon = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            Height = 50;

            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background4,
                    Alpha = 0.9f
                },
                // Play/Stop Button
                new ClickableContainer
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Margin = new MarginPadding { Left = 20 },
                    Size = new Vector2(80, 36),
                    Action = toggleGameState,
                    Masking = true,
                    CornerRadius = 18,
                    Children = new Drawable[]
                    {
                        playButtonBackground = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colours.Green
                        },
                        playButtonIcon = new SpriteIcon
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Icon = FontAwesome.Solid.Play,
                            Size = new Vector2(16),
                            Colour = Color4.White
                        }
                    }
                },
                // Tabs
                new FillFlowContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Direction = FillDirection.Horizontal,
                    AutoSizeAxes = Axes.Both,
                    Spacing = new Vector2(5),
                    Children = new Drawable[]
                    {
                        new IconButton
                        {
                            Icon = FontAwesome.Solid.Home,
                            Action = () => OnTabSelected?.Invoke(0)
                        },
                        new IconButton
                        {
                            Icon = FontAwesome.Solid.AlignLeft,
                            Action = () => OnTabSelected?.Invoke(1)
                        },
                        new IconButton
                        {
                            Icon = FontAwesome.Solid.Cog,
                            Action = () => OnTabSelected?.Invoke(2)
                        },
                        new IconButton
                        {
                            Icon = FontAwesome.Solid.InfoCircle,
                            Action = () => OnTabSelected?.Invoke(3)
                        }
                    }
                }
            };
        }

        private void toggleGameState()
        {
            if (runningGameProcess != null && !runningGameProcess.HasExited)
            {
                // Stop game
                runningGameProcess.Kill();
                runningGameProcess = null;
            }
            else
            {
                // Start game
                string osuDir = OsuCcPaths.ResolveOsuDirectory(null);
                string ccDataRoot = osucc.Common.OsuCcDataRootResolver.Resolve(osuDir);
                string hookDll = System.IO.Path.Combine(ccDataRoot, "hook", osucc.Common.OsuCcLayout.HookDllName);
                runningGameProcess = GameLauncher.Launch(osuDir, hookDll);

                if (runningGameProcess != null)
                {
                    runningGameProcess.EnableRaisingEvents = true;
                    runningGameProcess.Exited += (s, e) => Schedule(() => updatePlayButtonState());
                }
            }

            updatePlayButtonState();
        }

        private void updatePlayButtonState()
        {
            if (runningGameProcess != null && !runningGameProcess.HasExited)
            {
                playButtonBackground.Colour = colours.Red;
                playButtonIcon.Icon = FontAwesome.Solid.Stop;
            }
            else
            {
                playButtonBackground.Colour = colours.Green;
                playButtonIcon.Icon = FontAwesome.Solid.Play;
            }
        }
    }
}
