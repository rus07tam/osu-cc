using osu.Game.Users;
using System;

namespace FakeSupporter
{
    /// <summary>
    /// Public API of the Fake Supporter plugin, exported under the <c>fake-supporter</c> plugin id
    /// (<see cref="osucc.Plugin.IOsuCcPluginHost.ExportApi"/>, fetched with
    /// <see cref="osucc.Plugin.IOsuCcPluginHost.GetApi{T}"/>). Lets other plugins register
    /// supporter conditionals with priorities and set per-user supporter overrides. Everything is
    /// local cosmetic: no data is ever sent to the servers.
    /// </summary>
    public interface ISupporterFakerApi
    {
        /// <summary>Whether the current player's own supporter tag is faked (the plugin's master setting).</summary>
        bool Enabled { get; }

        /// <summary>The faked supporter level for the current player (1–10 hearts).</summary>
        int Level { get; }

        /// <summary>
        /// Registers a supporter conditional: when <paramref name="predicate"/> matches, the user is
        /// shown with a supporter tag (<paramref name="isSupporter"/>) at the given level (when
        /// <paramref name="level"/> is null, the plugin's current <see cref="Level"/> is used). When
        /// several rules match, the highest <paramref name="priority"/> wins; ties resolve to the
        /// later-registered rule. Dispose the returned handle to revoke the rule.
        /// </summary>
        IDisposable AddRule(Func<IUser, bool> predicate, bool isSupporter, int? level, int priority = 0);

        /// <summary>Sets a per-user supporter override (session-scoped; beats every rule and the persisted overrides). Clear it with <see cref="ClearUserSupporter"/>.</summary>
        void SetUserSupporter(int userId, SupporterOverrideMode mode, int? level);

        /// <summary>Clears the per-user supporter override set via <see cref="SetUserSupporter"/>.</summary>
        void ClearUserSupporter(int userId);

        /// <summary>
        /// Resolves whether a user is shown with a supporter tag, or <c>null</c> when nothing applies
        /// (the real supporter state is left untouched). A session override wins, then a persisted
        /// override, then the current player while <see cref="Enabled"/> is on, then the best matching
        /// rule by priority.
        /// </summary>
        bool? ResolveIsSupporter(IUser user);

        /// <summary>Resolves the supporter level shown for a user (0 when the user is not shown as a supporter).</summary>
        int ResolveLevel(IUser user);

        /// <summary>Fired whenever any rule, override or setting changes; live UI re-resolves.</summary>
        event Action? Changed;
    }
}
