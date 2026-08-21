using osucc.Core;
using osucc.Plugin;
using System;

namespace Oii
{
    /// <summary>
    /// oii: shows the improvement indicator (ii) — the ratio of expected playtime for the user's pp
    /// against their actual playtime — next to total play time on every user profile.
    public class OiiPlugin : OsuCcPlugin
    {
        public override IReadOnlyList<OsuCcPatch> Patches => new OsuCcPatch[]
        {
            new TotalPlayTimeLoadPatch(this, Host),
        };

        protected override void OnLoad()
        {
            int patched = InstallPatches();
            Host.Log(patched == 1 ? "patch installed" : "patch unavailable");
            Host.Log("loaded");
        }

        public override void AttachToGame()
        {
        }

        public override void Dispose()
        {
            TotalPlayTimeLoadPatch.RemoveIndicators();
            GC.SuppressFinalize(this);
            base.Dispose();
        }
    }
}
