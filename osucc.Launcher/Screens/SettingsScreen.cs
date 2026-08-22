using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Screens;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;

namespace osucc.Launcher.Screens
{
    public partial class SettingsScreen : Screen
    {
        [Resolved]
        private osu.Framework.Platform.GameHost host { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private osucc.Launcher.Configuration.LauncherConfigManager configManager { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            string osuDir = osucc.Launcher.Core.OsuCcPaths.ResolveOsuDirectory(null) ?? "";
            string osuDataRoot = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".local", "share", "osu");
            string ccDataRoot = osucc.Common.OsuCcDataRootResolver.Resolve(osuDir);

            InternalChild = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 15),
                Width = 500,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = "Settings",
                        Margin = new MarginPadding { Bottom = 10 },
                        Font = OsuFont.GetFont(size: 40, weight: FontWeight.Bold)
                    },
                    createConfigSetting("Update Repository (GitHub)", configManager.GetBindable<string>(osucc.Launcher.Configuration.LauncherSetting.UpdateRepository)),
                    createSetting("Game Folder (where osu! executable is)", osuDir),
                    createSetting("osu! Data Folder", osuDataRoot),
                    createSetting("osu-cc Data Folder", ccDataRoot)
                }
            };
        }

        private FillFlowContainer createConfigSetting(string label, Bindable<string> bindable)
        {
            var textBox = new OsuTextBox
            {
                RelativeSizeAxes = Axes.X,
                Current = bindable
            };

            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = label,
                        Colour = colourProvider.Content2,
                        Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold)
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 40,
                        Child = textBox
                    }
                }
            };
        }

        private FillFlowContainer createSetting(string label, string autoDetectedPath)
        {
            var textBox = new OsuTextBox
            {
                RelativeSizeAxes = Axes.X,
                PlaceholderText = string.IsNullOrEmpty(autoDetectedPath) ? "Not found" : autoDetectedPath
            };

            var openButton = new ShearedButton
            {
                Width = 80,
                Height = 40,
                Text = "Open",
                Action = () =>
                {
                    string target = string.IsNullOrEmpty(textBox.Text) ? autoDetectedPath : textBox.Text;
                    if (!string.IsNullOrEmpty(target) && System.IO.Directory.Exists(target))
                    {
                        host.OpenFileExternally(target);
                    }
                }
            };

            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = label,
                        Colour = colourProvider.Content2,
                        Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold)
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 40, // Matches the text box and button heights so they align.
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.Distributed),
                            new Dimension(GridSizeMode.Absolute, 10),
                            new Dimension(GridSizeMode.Absolute, 80)
                        },
                        Content = new[]
                        {
                            new Drawable[] { textBox, new Container(), openButton }
                        }
                    }
                }
            };
        }
    }
}
