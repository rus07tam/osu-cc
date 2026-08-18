using osu.Framework.Graphics.Sprites;
using osucc.Plugin;
using System;

namespace OsuCcUpdater
{
    /// <summary>
    /// The osu-cc updater plugin. Registers a settings subsection (source selector, auto-check,
    /// check/build buttons) and a toolbar button, and auto-checks once per launch. Whatever is
    /// staged here is applied by the osucc launcher on the next launch.
    /// </summary>
    public class OsuCcUpdaterPlugin : OsuCcPlugin
    {
        private OsuCcUpdaterApi? api;
        private IDisposable? toolbarHandle;

        /// <summary>The download icon, matching the plugin's toolbar button.</summary>
        public override IconUsage? Icon => FontAwesome.Solid.CloudDownloadAlt;

        protected override void OnLoad()
        {
            var settings = Host.GetSettings();

            api = new OsuCcUpdaterApi(Host, settings);
            Host.ExportApi(api);
            Host.Log("exported public api");

            Host.AddSettingsSubsection(() => new UpdaterSettingsSubsection(api!));

            toolbarHandle = Host.AddToolbarButton(() => new UpdaterToolbarButton(checkFromToolbar), ToolbarButtonPlacement.Right);

            Host.Log("loaded");
        }

        public override void AttachToGame()
        {
            api?.AutoCheckIfDue();
        }

        private void checkFromToolbar()
        {
            if (api == null || api.Busy)
                return;

            _ = Task.Run(() => api.RunAndNotifyAsync(api.Source.Value));
        }

        public override void Dispose()
        {
            toolbarHandle?.Dispose();
            toolbarHandle = null;
            api?.Dispose();
            api = null;
            GC.SuppressFinalize(this);
            base.Dispose();
        }
    }
}