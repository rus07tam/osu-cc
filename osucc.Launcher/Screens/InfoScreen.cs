using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;
using System.Reflection;
using System.Runtime.InteropServices;

namespace osucc.Launcher.Screens
{
    public partial class InfoScreen : Screen
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
            string ccDataRoot = osucc.Common.OsuCcDataRootResolver.Resolve(osuDir);
            string hookDirectory = osucc.Common.OsuCcDataRootResolver.ResolveHookDirectory(ccDataRoot);
            
            string currentRepo = configManager.Get<string>(osucc.Launcher.Configuration.LauncherSetting.UpdateRepository);

            InternalChild = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 6),
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = "Info",
                        Margin = new MarginPadding { Bottom = 10 },
                        Font = OsuFont.GetFont(size: 40, weight: FontWeight.Bold)
                    },
                    createLinkRow("GitHub", $"https://github.com/{currentRepo}"),
                    createLinkRow("Documentation", $"https://github.com/{currentRepo}#documentation"),
                    createSectionHeader("Versions"),
                    createValueRow("osucc.Host", readVersion(Path.Combine(hookDirectory, "osucc.dll"))),
                    createValueRow("osucc.Api", readVersion(Path.Combine(hookDirectory, "osucc.Api.dll"))),
                    createValueRow("osucc.Launcher", launcherVersion),
                    createSectionHeader("Runtime"),
                    createValueRow("Commit", readCommitHash()),
                    createValueRow("OS", RuntimeInformation.OSDescription),
                    createValueRow("Architecture", RuntimeInformation.ProcessArchitecture.ToString()),
                    createValueRow(".NET", Environment.Version.ToString())
                }
            };
        }

        private static string launcherVersion
        {
            get
            {
                Version? version = Assembly.GetExecutingAssembly().GetName().Version;
                return version == null ? "unknown" : FormatVersion(version.ToString());
            }
        }

        private static string readVersion(string dllPath)
        {
            string? version = osucc.Common.OsuCcVersionReader.Read(dllPath);
            return string.IsNullOrEmpty(version) ? "not installed" : FormatVersion(version);
        }

        private static string FormatVersion(string value)
        {
            // FileVersion carries a trailing ".0" from the 3-component <Version>; trim it for display.
            while (value.EndsWith(".0", StringComparison.Ordinal))
                value = value[..^2];

            return value;
        }

        private static string readCommitHash()
        {
            string? informational = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            if (string.IsNullOrEmpty(informational))
                return "unknown";

            int hashIndex = informational.IndexOf('+');
            string hash = hashIndex >= 0 ? informational[(hashIndex + 1)..] : informational;

            return hash.Length > 7 ? hash[..7] : hash;
        }

        private OsuSpriteText createSectionHeader(string text)
        {
            var header = new OsuSpriteText
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Text = text.ToUpperInvariant(),
                Colour = colourProvider.Content2,
                Margin = new MarginPadding { Top = 12, Bottom = 2 },
                Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold)
            };

            return header;
        }

        private GridContainer createValueRow(string label, string value)
        {
            return new GridContainer
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Width = 380,
                AutoSizeAxes = Axes.Y,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize)
                },
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 160),
                    new Dimension(GridSizeMode.Absolute, 10),
                    new Dimension()
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = label,
                            Colour = colourProvider.Content2,
                            Font = OsuFont.GetFont(size: 20, weight: FontWeight.SemiBold)
                        },
                        new Container(),
                        new OsuSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = value,
                            Colour = colourProvider.Content1,
                            Font = OsuFont.GetFont(size: 20)
                        }
                    }
                }
            };
        }

        private OsuClickableContainer createLinkRow(string label, string url)
        {
            return new OsuClickableContainer
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                AutoSizeAxes = Axes.Both,
                Action = () => host.OpenUrlExternally(url),
                Child = new OsuSpriteText
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Text = label,
                    Colour = colourProvider.Highlight1,
                    Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold)
                }
            };
        }
    }
}
