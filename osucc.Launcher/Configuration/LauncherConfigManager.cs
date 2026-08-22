using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace osucc.Launcher.Configuration
{
    public class LauncherConfigManager : IniConfigManager<LauncherSetting>
    {
        protected override string Filename => "launcher.ini";

        public LauncherConfigManager(Storage storage)
            : base(storage)
        {
        }

        protected override void InitialiseDefaults()
        {
            SetDefault(LauncherSetting.UpdateRepository, osucc.Common.Update.OsuCcUpdateService.DefaultRepository);
        }
    }

    public enum LauncherSetting
    {
        UpdateRepository
    }
}
