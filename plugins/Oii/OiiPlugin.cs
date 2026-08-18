using osucc.Plugin;
using System;

namespace Oii
{
    /// <summary>
    /// oii: shows the improvement indicator (ii) — the ratio of expected playtime for the user's pp
    /// against their actual playtime — next to total play time on every user profile.
    /// </summary>
    public class OiiPlugin : OsuCcPlugin
    {
        private IDisposable? patch;

        protected override void OnLoad()
        {
            patch = TotalPlayTimeLoadPatch.Install(Host);
            Host.Log(patch != null ? "patch installed" : "patch unavailable");
            Host.Log("loaded");
        }

        public override void AttachToGame()
        {
        }

        public override void Dispose()
        {
            patch?.Dispose();
            TotalPlayTimeLoadPatch.RemoveIndicators();
            GC.SuppressFinalize(this);
            base.Dispose();
        }
    }
}
