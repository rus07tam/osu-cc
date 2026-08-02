using osucc.Plugin;

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
        public void Load(IOsuCcPluginHost host)
        {
            var harmony = host.CreateHarmony("oii");
            host.Log(TotalPlayTimeLoadPatch.Install(harmony) ? "patch installed" : "patch unavailable");
            host.Log("loaded");
        }

        public void AttachToGame()
        {
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
