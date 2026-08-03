using osu.Framework.Graphics;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Users;

namespace UsernameVisuals
{
    /// <summary>How a username should be rendered, as resolved by a display rule.</summary>
    public enum UsernameNameMode
    {
        /// <summary>Show the real username.</summary>
        Normal,

        /// <summary>Show <see cref="UsernameNameRule.Text"/> instead of the real username.</summary>
        Replace,

        /// <summary>Hide the username behind a solid block.</summary>
        Hide,
    }

    /// <summary>A display rule: a mode plus the replacement text for <see cref="UsernameNameMode.Replace"/>.</summary>
    public readonly record struct UsernameNameRule(UsernameNameMode Mode, string? Text)
    {
        public static UsernameNameRule Normal => new(UsernameNameMode.Normal, null);

        public static UsernameNameRule Replace(string text) => new(UsernameNameMode.Replace, text);

        public static UsernameNameRule Hide => new(UsernameNameMode.Hide, null);
    }

    /// <summary>Inputs a username conditional evaluates against.</summary>
    public interface IUsernameVisualsContext
    {
        IUser? User { get; }

        APIUser? LocalUser { get; }
    }

    /// <summary>
    /// Public API of the Username Visuals plugin, exported under the <c>username-visuals</c> plugin
    /// id (<see cref="osucc.Plugin.IOsuCcPluginHost.ExportApi"/>, fetched with
    /// <see cref="osucc.Plugin.IOsuCcPluginHost.GetApi{T}"/>). Lets other plugins register colour and
    /// display-name conditionals with priorities and set per-user overrides.
    /// </summary>
    public interface IUsernameVisualsApi
    {
        /// <summary>Whether gradient rendering is enabled at all (the plugin's master setting).</summary>
        bool Enabled { get; }

        /// <summary>
        /// Registers a colour conditional: when <paramref name="predicate"/> matches, the user's
        /// username is drawn with <paramref name="palette"/>. When several rules match, the highest
        /// <paramref name="priority"/> wins; ties resolve to the later-registered rule. Dispose the
        /// returned handle to revoke the rule. The plugin's own "others" fallback uses the lowest
        /// priority, so plugin rules naturally override it; the plugin's own-username display
        /// settings (hide, replace) always win, and its own-username palette sits at priority 0.
        /// </summary>
        IDisposable AddColourRule(Func<IUsernameVisualsContext, bool> predicate, IReadOnlyList<Colour4> palette, int priority = 0);

        /// <summary>Registers a display-name conditional (override / hide) with the same priority rules as <see cref="AddColourRule"/>. Dispose the returned handle to revoke it.</summary>
        IDisposable AddNameRule(Func<IUsernameVisualsContext, bool> predicate, UsernameNameRule rule, int priority = 0);

        /// <summary>Sets a per-user gradient colour (session-scoped; beats every registered colour rule). Clear it with <see cref="ClearUserColour"/>.</summary>
        void SetUserColour(int userId, IReadOnlyList<Colour4> palette);

        /// <summary>Clears the per-user colour set via <see cref="SetUserColour"/>.</summary>
        void ClearUserColour(int userId);

        /// <summary>Sets a per-user display override (session-scoped; beats every registered name rule). Pass <see cref="UsernameNameRule.Normal"/> to clear it.</summary>
        void SetUserName(int userId, UsernameNameRule rule);

        /// <summary>Clears the per-user display override set via <see cref="SetUserName"/>.</summary>
        void ClearUserName(int userId);

        /// <summary>Resolves the effective gradient palette for a user, or <c>null</c> for normal single-colour rendering.</summary>
        IReadOnlyList<Colour4>? ResolveColour(IUser? user, APIUser? localUser);

        /// <summary>Resolves the effective display rule for a user.</summary>
        UsernameNameRule ResolveName(IUser? user, APIUser? localUser);

        /// <summary>Fired whenever any rule, override or setting changes; username texts re-resolve.</summary>
        event Action? Changed;
    }
}
