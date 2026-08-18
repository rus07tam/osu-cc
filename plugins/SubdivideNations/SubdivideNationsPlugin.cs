using osu.Framework.Bindables;
using osucc.Plugin;
using System;

namespace SubdivideNations
{
    /// <summary>
    /// Subdivide Nations: shows each user's sub-national region on the profile header and on every
    /// user panel/card, resolving regions through the osuworld API (the same source the
    /// osu-subdivide-nations web extension uses). Region names come from an embedded dataset;
    /// region flags are loaded as PNG thumbnails when available and degrade to name-only otherwise.
    /// </summary>
    public class SubdivideNationsPlugin : OsuCcPlugin
    {
        private IOsuCcPluginHost host = null!;
        private PluginSettings settings = null!;
        private Bindable<bool> enabled = null!;
        private Bindable<bool> showFlags = null!;
        private IDisposable? userPanelPatch;
        private IDisposable? headerPatch;

        protected override void OnLoad()
        {

            settings = host.GetSettings();
            enabled = settings.Bind("enabled", true);
            showFlags = settings.Bind("show_flags", true);

            RegionService.SetEnabled(() => enabled.Value);
            RegionFlagStore.SetShowFlags(() => showFlags.Value);

            host.AddSettingsSubsection(() => new SubdivideNationsSettingsSubsection(settings));

            int patched = installPatches();
            host.Log(patched == 2 ? "patches installed" : $"patched {patched}/2 surfaces");
            host.Log("loaded");
        }

        public override void AttachToGame()
        {
            var storage = host.GetStorage();
            RegionService.Attach(storage);
            RegionFlagStore.Attach(storage);
            host.Log("attached");
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            base.Dispose();
            userPanelPatch?.Dispose();
            headerPatch?.Dispose();
            settings?.Dispose();
        }

        private int installPatches()
        {
            int count = 0;
            if (UserPanelCreateFlagPatch.Install(host) is { } userPanel) { userPanelPatch = userPanel; count++; }
            if (TopHeaderContainerPatch.Install(host) is { } header) { headerPatch = header; count++; }

            return count;
        }
    }
}
