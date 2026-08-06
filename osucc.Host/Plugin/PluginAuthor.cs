namespace osucc.Plugin
{
    /// <summary>
    /// A single plugin author. Either a plain nickname (<see cref="OsuesId"/> is <c>null</c>) or
    /// an osu! profile owner — when <see cref="OsuesId"/> is set, the UI renders the author as a
    /// clickable username linking to <c>https://osu.ppy.sh/users/{id}</c>.
    /// </summary>
    public sealed class PluginAuthor
    {
        /// <summary>Display label: the nickname, or the username for a profile-linked author.</summary>
        public string Name { get; }

        /// <summary>osu! profile id; <c>null</c> means this is a plain nickname, not a profile link.</summary>
        public int? OsuesId { get; }

        public PluginAuthor(string name)
        {
            Name = name;
        }

        public PluginAuthor(string name, int osuProfileId)
        {
            Name = name;
            OsuesId = osuProfileId;
        }
    }
}
