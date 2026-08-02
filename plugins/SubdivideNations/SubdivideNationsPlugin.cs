using osu.Framework.Bindables;
using osucc.Plugin;

namespace SubdivideNations
{
    /// <summary>
    /// Subdivide Nations: shows each user's sub-national region on the profile header and on every
    /// user panel/card, resolving regions through the osuworld API (the same source the
    /// osu-subdivide-nations web extension uses). Region names come from an embedded dataset;
    /// region flags are loaded as PNG thumbnails when available and degrade to name-only otherwise.
    /// </summary>
    [OsuCcPlugin(
        "subdivide-nations",
        "Subdivide Nations",
        Author = "osu-cc",
        Description = "Shows each user's sub-national region on profiles and user cards.",
        Version = "1.0.0")]
    public class SubdivideNationsPlugin : IOsuCcPlugin
    {
        private IOsuCcPluginHost host = null!;
        private PluginSettings settings = null!;
        private Bindable<bool> enabled = null!;
        private Bindable<bool> showFlags = null!;

        public void Load(IOsuCcPluginHost host)
        {
            this.host = host;

            settings = host.GetSettings();
            enabled = settings.Bind("subdivide_enabled", true);
            showFlags = settings.Bind("show_flags", true);

            RegionService.SetEnabled(() => enabled.Value);
            RegionFlagStore.SetShowFlags(() => showFlags.Value);

            host.AddSettingsSubsection(() => new SubdivideNationsSettingsSubsection(settings));

            int patched = installPatches();
            host.Log(patched == 2 ? "patches installed" : $"patched {patched}/2 surfaces");
            host.Log("loaded");
        }

        public void AttachToGame()
        {
            var storage = host.GetStorage();
            RegionService.Attach(storage);
            RegionFlagStore.Attach(storage);
            host.Log("attached");
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            settings?.Dispose();
        }

        private int installPatches()
        {
            var harmony = host.CreateHarmony("subdivide-nations");

            int count = 0;
            if (UserPanelCreateFlagPatch.Install(harmony)) count++;
            if (TopHeaderContainerPatch.Install(harmony)) count++;

            return count;
        }
    }
}
