using osu.Framework;
using osu.Framework.Platform;

namespace osucc.Launcher.Desktop
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (DesktopGameHost host = Host.GetSuitableDesktopHost(@"osu-cc-launcher"))
            using (var game = new OsuCcLauncherGame())
                host.Run(game);
        }
    }
}
