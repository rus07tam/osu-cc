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
        private PluginSettings settings = null!;
        private Bindable<bool> enabled = null!;
        private Bindable<bool> showFlags = null!;
        private IDisposable? userPanelPatch;
        private IDisposable? headerPatch;

        protected override void OnLoad()
        {

            settings = Host.GetSettings();
            enabled = settings.Bind("enabled", true);
            showFlags = settings.Bind("show_flags", true);

            RegionService.SetEnabled(() => enabled.Value);
            RegionFlagStore.SetShowFlags(() => showFlags.Value);

            Host.AddSettingsSubsection(() => new SubdivideNationsSettingsSubsection(settings));

            int patched = installPatches();
            Host.Log(patched == 2 ? "patches installed" : $"patched {patched}/2 surfaces");
            Host.Log("loaded");
        }

        public override void AttachToGame()
        {
            var storage = Host.GetStorage();
            RegionService.Attach(storage, Host);
            RegionFlagStore.Attach(storage, Host);
            Host.Log("attached");
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
            if (UserPanelCreateFlagPatch.Install(Host) is { } userPanel) { userPanelPatch = userPanel; count++; }
            if (TopHeaderContainerPatch.Install(Host) is { } header) { headerPatch = header; count++; }

            return count;
        }
    }
}
