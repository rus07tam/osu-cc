using osu.Game.Online.API.Requests.Responses;
using osu.Game.Users;
using System;

namespace CustomUserGroups
{
    /// <summary>
    /// Public API of the Custom User Groups plugin, exported under the <c>custom-user-groups</c>
    /// plugin id (<see cref="osucc.Plugin.IOsuCcPluginHost.ExportApi"/>, fetched with
    /// <see cref="osucc.Plugin.IOsuCcPluginHost.GetApi{T}"/>). Lets other plugins define groups,
    /// register group conditionals with priorities and set per-user group overrides. Everything is
    /// local cosmetic: no data is ever sent to the servers.
    /// </summary>
    public interface ICustomUserGroupsApi
    {
        /// <summary>Whether custom groups are applied at all (the plugin's master setting).</summary>
        bool Enabled { get; }

        /// <summary>All currently defined groups: the settings-defined library plus the ones registered via <see cref="RegisterGroup"/>.</summary>
        IReadOnlyList<CustomUserGroup> Groups { get; }

        /// <summary>Fired whenever any group, rule, override or setting changes; live UI re-resolves.</summary>
        event Action? Changed;

        /// <summary>
        /// Registers a group definition so it can be assigned by id. Returns a handle whose dispose
        /// unregisters the group (and the assignments referring to it resolve to nothing).
        /// </summary>
        IDisposable RegisterGroup(CustomUserGroup group);

        /// <summary>
        /// Registers a group conditional: when <paramref name="predicate"/> matches, the user is shown
        /// in the group with the given <paramref name="groupId"/>. When several rules match, the highest
        /// <paramref name="priority"/> wins; ties resolve to the later-registered rule. Dispose the
        /// returned handle to revoke the rule. Per-user overrides (session and persisted) beat rules.
        /// </summary>
        IDisposable AddGroupRule(Func<IUser, bool> predicate, int groupId, int priority = 0);

        /// <summary>Sets a per-user group override (session-scoped; beats every rule and the persisted overrides). Clear it with <see cref="ClearUserGroup"/>.</summary>
        void SetUserGroup(int userId, int groupId);

        /// <summary>Clears the per-user group override set via <see cref="SetUserGroup"/>.</summary>
        void ClearUserGroup(int userId, int groupId);

        /// <summary>Clears all per-user group overrides set via <see cref="SetUserGroup"/> for a user.</summary>
        void ClearUserGroup(int userId);

        /// <summary>Adds or updates a persisted per-user group override (settings-driven; beats rules but loses to session overrides).</summary>
        void SetPersistedOverride(UserGroupOverride userOverride);

        /// <summary>Removes a specific persisted per-user group override.</summary>
        void RemovePersistedOverride(int userId, int groupId);

        /// <summary>Removes all persisted per-user group overrides for a user.</summary>
        void RemovePersistedOverride(int userId);

        /// <summary>The currently persisted per-user group overrides (settings-driven), for display.</summary>
        IReadOnlyList<UserGroupOverride> PersistedOverrides { get; }

        /// <summary>
        /// Resolves the effective group badges for a user: the user's real groups (from the API
        /// response) with the resolved custom group appended when one applies.
        /// </summary>
        IReadOnlyList<APIUserGroup>? ResolveGroups(IUser? user);

        /// <summary>
        /// Resolves the primary username colour a custom group gives a user, or <c>null</c> when no
        /// custom group applies or the group has no colour.
        /// </summary>
        string? ResolveColour(IUser? user);
    }
}
