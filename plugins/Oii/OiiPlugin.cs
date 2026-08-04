using osucc.Plugin;
using System;

namespace Oii
{
    /// <summary>
    /// oii: shows the improvement indicator (ii) — the ratio of expected playtime for the user's pp
    /// against their actual playtime — next to total play time on every user profile.
    /// </summary>
    [OsuCcPlugin(
        "oii",
        "oii",
        Author = "osu-cc",
        Description = "Shows the improvement indicator next to total play time on user profiles.",
        Version = "1.0.0")]
    public class OiiPlugin : IOsuCcPlugin
    {
        private IDisposable? patch;

        public void Load(IOsuCcPluginHost host)
        {
            patch = TotalPlayTimeLoadPatch.Install(host);
            host.Log(patch != null ? "patch installed" : "patch unavailable");
            host.Log("loaded");
        }

        public void AttachToGame()
        {
        }

        public void Dispose()
        {
            patch?.Dispose();
            TotalPlayTimeLoadPatch.RemoveIndicators();
            GC.SuppressFinalize(this);
        }
    }
}
