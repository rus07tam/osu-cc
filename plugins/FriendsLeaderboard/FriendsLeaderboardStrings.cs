using osu.Framework.Localisation;
using osucc.Localisation;

namespace FriendsLeaderboard
{
    public static class FriendsLeaderboardStrings
    {
        private const string prefix = "friends-leaderboard";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString Name => OsuCcLocalisation.Get($"{prefix}:name", "Friends Leaderboard");

        public static LocalisableString Description => OsuCcLocalisation.Get($"{prefix}:description", "Shows the friend leaderboard without an osu!supporter tag by aggregating each friend's best score client-side.");

        public static LocalisableString EnableCaption => OsuCcLocalisation.Get(getKey(nameof(EnableCaption)), "Enable");

        public static LocalisableString EnableHint => OsuCcLocalisation.Get(getKey(nameof(EnableHint)), "Show the friend leaderboard without an osu!supporter tag, built from each friend's best score on the beatmap.");
    }
}
