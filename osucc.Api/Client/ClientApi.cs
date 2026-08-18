using osu.Game;
using osucc.Core;

namespace osucc.Client
{
    public static class ClientApi
    {
        public const string BrandingName = "osu!cc";
        public static OsuGameBase? Game { get; set; }
        public static string? OriginalGameName { get; set; }
    }
}
