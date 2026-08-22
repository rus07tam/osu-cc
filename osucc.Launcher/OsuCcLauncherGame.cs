using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Screens;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Overlays;
using osucc.Common.Update;
using osucc.Launcher.Screens;
using osucc.Launcher.UI;
using osuTK.Graphics;

namespace osucc.Launcher
{
    public partial class OsuCcLauncherGame : osu.Game.OsuGameBase
    {
        private ScreenStack screenStack = null!;

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        private osucc.Launcher.Configuration.LauncherConfigManager configManager = null!;
        private OsuCcUpdateService updateService = null!;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

            string osuDir = osucc.Launcher.Core.OsuCcPaths.ResolveOsuDirectory(null);
            string ccDataRoot = osucc.Common.OsuCcDataRootResolver.Resolve(osuDir);

            configManager = new osucc.Launcher.Configuration.LauncherConfigManager(Host.Storage);
            dependencies.CacheAs(configManager);

            updateService = new OsuCcUpdateService(ccDataRoot, configManager.Get<string>(osucc.Launcher.Configuration.LauncherSetting.UpdateRepository));
            dependencies.CacheAs(updateService);

            return dependencies;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            configManager.GetBindable<string>(osucc.Launcher.Configuration.LauncherSetting.UpdateRepository).BindValueChanged(v => updateService.Repository = v.NewValue, true);

            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background6
                    },
                    new Triangles
                    {
                        RelativeSizeAxes = Axes.Both,
                        ColourLight = colourProvider.Background5,
                        ColourDark = colourProvider.Background6,
                        TriangleScale = 2f
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Bottom = 50 },
                        Child = screenStack = new ScreenStack
                        {
                            RelativeSizeAxes = Axes.Both,
                        }
                    },
                    new BottomToolbar
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        OnTabSelected = index =>
                        {
                            switch (index)
                            {
                                case 0: navigateTo(new MainScreen()); break;
                                case 1: navigateTo(new LogsScreen()); break;
                                case 2: navigateTo(new SettingsScreen()); break;
                                case 3: navigateTo(new InfoScreen()); break;
                            }
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            screenStack.Push(new MainScreen());
        }

        private void navigateTo(Screen screen)
        {
            if (screenStack.CurrentScreen?.GetType() == screen.GetType()) return;
            screenStack.CurrentScreen?.Exit();
            screenStack.Push(screen);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            updateService?.Dispose();
        }
    }
}
