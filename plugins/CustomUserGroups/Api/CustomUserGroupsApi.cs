using osu.Framework.Bindables;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Users;
using osucc.Client;
using osucc.Core;
using osucc.Plugin;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace CustomUserGroups
{
    /// <summary>
    /// A user group definition with the same key properties as <see cref="APIUserGroup"/>, editable
    /// in the plugin settings and assignable to users via rules and per-user overrides.
    /// </summary>
    public sealed class CustomUserGroup
    {
        /// <summary>A locally unique numeric id; referenced by rules and overrides.</summary>
        public int Id { get; set; }

        /// <summary>Stable identifier; defaults to <see cref="ShortName"/> when empty.</summary>
        public string? Identifier { get; set; }

        /// <summary>Full group name, shown in the badge tooltip.</summary>
        public string? Name { get; set; }

        /// <summary>Short name rendered inside the badge (e.g. "GMT").</summary>
        public string? ShortName { get; set; }

        /// <summary>Badge and username colour as a hex string; empty or <c>null</c> for none.</summary>
        public string? Colour { get; set; }

        /// <summary>Whether the badge is drawn at reduced opacity, like real probationary groups.</summary>
        public bool IsProbationary { get; set; }

        /// <summary>Optional ruleset short names (e.g. "osu", "taiko") shown as icons in the badge.</summary>
        public string[]? Playmodes { get; set; }

        public APIUserGroup ToAPIUserGroup() => new()
        {
            Id = Id,
            Identifier = string.IsNullOrEmpty(Identifier) ? ShortName ?? string.Empty : Identifier,
            Name = Name ?? string.Empty,
            ShortName = ShortName ?? string.Empty,
            Colour = Colour,
            IsProbationary = IsProbationary,
            Playmodes = Playmodes,
        };
    }

    /// <summary>A per-user group override persisted in the plugin's <c>user_overrides</c> setting.</summary>
    public sealed class UserGroupOverride
    {
        /// <summary>The osu! user id this override applies to.</summary>
        public int UserId { get; set; }

        /// <summary>The custom group id to show for this user.</summary>
        public int GroupId { get; set; }
    }

    /// <summary>
    /// Concrete <see cref="ICustomUserGroupsApi"/>: holds the plugin's group library (settings +
    /// API-registered), the rules registered by other plugins and the per-user overrides (session +
    /// persisted), and resolves the effective groups / colour for a user. The plugin exports this
    /// instance under the <c>custom-user-groups</c> id and its patches stamp every API response
    /// through it.
    /// </summary>
    public sealed class CustomUserGroupsApi : ICustomUserGroupsApi
    {
        private const string enabledKey = "enabled";
        private const string groupsKey = "groups";
        private const string userOverridesKey = "user_overrides";
        private const int maxWalkDepth = 12;

        /// <summary>The singleton the plugin exports and its patches resolve through.</summary>
        public static CustomUserGroupsApi Instance { get; internal set; } = null!;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly object lockObject = new();

        private readonly List<GroupRule> rules = new();
        private readonly List<RegisteredGroup> registeredGroups = new();
        private readonly Dictionary<int, List<int>> sessionOverrides = new();
        private readonly Dictionary<int, List<int>> persistedOverrides = new();
        private readonly List<CustomUserGroup> persistedGroups = new();
        private readonly List<UserGroupOverride> persistedList = new();

        private long nextRuleId;
        private long nextRegistrationId;

        private Bindable<bool> enabled = new(false);
        private Bindable<bool> applyUsernameColour = new(true);
        private Bindable<string> groups = new(string.Empty);
        private Bindable<string> userOverrides = new(string.Empty);

        // Read from background request threads and written from the update thread; plain int reads
        // are atomic, and the worst a race can produce is a response stamped one tick late.
        private int localId;
        private Bindable<APIUser>? localUser;

        /// <inheritdoc />
        public bool Enabled => enabled.Value;

        /// <inheritdoc />
        public event Action? Changed;

        /// <inheritdoc />
        public IReadOnlyList<CustomUserGroup> Groups
        {
            get
            {
                lock (lockObject)
                    return persistedGroups.Concat(registeredGroups.Select(r => r.Group)).ToArray();
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<UserGroupOverride> PersistedOverrides
        {
            get
            {
                lock (lockObject)
                    return persistedList.ToArray();
            }
        }

        /// <summary>The settings-defined group library (without API-registered groups), for the settings editor.</summary>
        public IReadOnlyList<CustomUserGroup> PersistedGroups
        {
            get
            {
                lock (lockObject)
                    return persistedGroups.ToArray();
            }
        }

        /// <summary>Replaces the settings-defined group library, persisting it and re-resolving live users.</summary>
        public void SetGroups(IReadOnlyList<CustomUserGroup> newGroups)
        {
            string json = JsonSerializer.Serialize(newGroups.Where(g => g != null && g.Id > 0).ToList(), jsonOptions);
            groups.Value = json;

            // Re-apply even when the serialized value round-trips to the same string (no ValueChanged).
            applyPersistedGroups();
        }

        /// <summary>Removes a group from the settings library, also pruning any persisted overrides that referenced it.</summary>
        public void RemoveGroup(int groupId)
        {
            bool pruned = false;

            lock (lockObject)
                pruned = persistedList.RemoveAll(o => o.GroupId == groupId) > 0;

            SetGroups(persistedGroups.Where(g => g.Id != groupId).ToList());

            if (pruned)
                writePersistedOverrides();
        }

        /// <summary>Wires the API to the plugin's persisted settings. Called once during <see cref="IOsuCcPlugin.Load"/>.</summary>
        public void Attach(PluginSettings settings)
        {
            enabled = settings.Bind(enabledKey, false);
            applyUsernameColour = settings.Bind("apply_username_colour", true);
            groups = settings.Bind(groupsKey, string.Empty);
            userOverrides = settings.Bind(userOverridesKey, string.Empty);

            enabled.ValueChanged += _ => onEnabledChanged();
            applyUsernameColour.ValueChanged += _ => onEnabledChanged();
            groups.ValueChanged += _ => applyPersistedGroups();
            userOverrides.ValueChanged += _ => applyPersistedOverrides();

            // Any library / override / rule change re-stamps the currently open profile so already
            // displayed badges (which would otherwise stay stale until the next re-fetch) update live.
            Changed += onStateChanged;

            applyPersistedGroups();
            applyPersistedOverrides();
        }

        /// <summary>Re-stamps the open profile on the update thread; the <c>Changed</c> event can fire from request threads.</summary>
        private void onStateChanged()
        {
            var game = ClientApi.Game;
            if (game == null)
                return;

            var scheduler = Reflection.GetScheduler(game);
            if (scheduler != null)
                scheduler.AddOnce(applyToAllCachedUsers);
            else
                applyToAllCachedUsers();
        }

        private void applyToAllCachedUsers()
        {
            foreach (var kvp in realStates)
            {
                stamp(kvp.Key);
            }

            applyOpenProfile();
        }

        /// <inheritdoc />
        public IDisposable RegisterGroup(CustomUserGroup group)
        {
            ArgumentNullException.ThrowIfNull(group);

            long handleId;

            lock (lockObject)
            {
                handleId = ++nextRegistrationId;
                registeredGroups.Add(new RegisteredGroup(handleId, group));
            }

            Changed?.Invoke();
            return new GroupRegistrationHandle(this, handleId);
        }

        /// <inheritdoc />
        public IDisposable AddGroupRule(Func<IUser, bool> predicate, int groupId, int priority = 0)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            long id;

            lock (lockObject)
            {
                id = ++nextRuleId;
                rules.Add(new GroupRule(id, predicate, groupId, priority));
            }

            Changed?.Invoke();
            return new RuleHandle(this, id);
        }

        /// <inheritdoc />
        public void SetUserGroup(int userId, int groupId)
        {
            if (userId <= 0 || findGroup(groupId) == null)
                return;

            lock (lockObject)
            {
                if (!sessionOverrides.TryGetValue(userId, out var list))
                    sessionOverrides[userId] = list = new List<int>();
                if (!list.Contains(groupId))
                    list.Add(groupId);
            }

            Changed?.Invoke();
        }

        /// <inheritdoc />
        public void ClearUserGroup(int userId, int groupId)
        {
            lock (lockObject)
            {
                if (sessionOverrides.TryGetValue(userId, out var list))
                {
                    list.Remove(groupId);
                    if (list.Count == 0)
                        sessionOverrides.Remove(userId);
                }
            }

            Changed?.Invoke();
        }

        /// <inheritdoc />
        public void ClearUserGroup(int userId)
        {
            lock (lockObject)
            {
                if (!sessionOverrides.Remove(userId))
                    return;
            }

            Changed?.Invoke();
        }

        /// <inheritdoc />
        public void SetPersistedOverride(UserGroupOverride userOverride)
        {
            if (userOverride == null || userOverride.UserId <= 0 || findGroup(userOverride.GroupId) == null)
                return;

            lock (lockObject)
            {
                if (!persistedList.Any(o => o.UserId == userOverride.UserId && o.GroupId == userOverride.GroupId))
                    persistedList.Add(userOverride);
            }

            writePersistedOverrides();
        }

        /// <inheritdoc />
        public void RemovePersistedOverride(int userId, int groupId)
        {
            lock (lockObject)
            {
                if (persistedList.RemoveAll(o => o.UserId == userId && o.GroupId == groupId) == 0)
                    return;
            }

            writePersistedOverrides();
        }

        /// <inheritdoc />
        public void RemovePersistedOverride(int userId)
        {
            lock (lockObject)
            {
                if (persistedList.RemoveAll(o => o.UserId == userId) == 0)
                    return;
            }

            writePersistedOverrides();
        }

        /// <inheritdoc />
        public IReadOnlyList<APIUserGroup>? ResolveGroups(IUser? user)
        {
            if (user == null)
                return null;

            var custom = resolveCustomGroups(user);

            if (custom.Count == 0)
                return realGroupsOf(user);

            var real = realGroupsOf(user) ?? Array.Empty<APIUserGroup>();
            var list = real.ToList();

            foreach (var g in custom)
            {
                list.RemoveAll(x => x.Id == g.Id);
                list.Add(g.ToAPIUserGroup());
            }

            return list;
        }

        /// <inheritdoc />
        public string? ResolveColour(IUser? user)
        {
            if (!applyUsernameColour.Value)
                return null;
            var custom = user == null ? null : resolveCustomGroups(user);
            return custom?.FirstOrDefault(g => !string.IsNullOrEmpty(g.Colour))?.Colour;
        }

        /// <summary>Called from the <c>LocalUserState.SetLocalUser</c> postfix once the real /me response is installed (update thread).</summary>
        public void OnLocalUserSet(IBindable<APIUser> user)
        {
            APIUser me = user.Value;
            if (me == null)
                return;

            localId = me.Id;
            localUser = user as Bindable<APIUser>;

            applyLocalUser();
        }

        /// <summary>Called from the <c>LocalUserState.ClearLocalUser</c> postfix (logout): forget the cached user.</summary>
        public void OnLocalUserCleared()
        {
            localId = 0;
            localUser = null;
        }

        /// <summary>
        /// Stamps every user inside a freshly deserialized API response. Called from the
        /// <c>APIRequest.Perform</c> postfix, which covers all /api/v2 JSON responses. The /me
        /// response (<see cref="APIMe"/>) is skipped here — it is handled by <see cref="OnLocalUserSet"/>.
        /// </summary>
        public void ApplyToResponse(object? root)
        {
            if (root == null || root is APIMe || !HasActiveState)
                return;

            walk(root, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
        }

        private bool HasActiveState => (enabled.Value && rules.Count > 0) || sessionOverrides.Count > 0 || persistedOverrides.Count > 0;

        private void removeRule(long id)
        {
            lock (lockObject)
                rules.RemoveAll(r => r.Id == id);

            Changed?.Invoke();
        }

        private void unregisterGroup(long handleId)
        {
            lock (lockObject)
                registeredGroups.RemoveAll(r => r.HandleId == handleId);

            Changed?.Invoke();
        }

        private void onEnabledChanged()
        {
            applyLocalUser();

            Changed?.Invoke();
        }

        private void applyLocalUser()
        {
            var user = localUser?.Value;
            if (user == null)
                return;

            if (!stamp(user))
                return;

            triggerPropagated(localUser!);
            applyOpenProfile();
        }

        /// <summary>
        /// Resolves the custom groups for a user: session overrides, then persisted overrides,
        /// then the matching rules by priority. Explicit per-user overrides always apply; the
        /// master toggle only gates the automatic rules. Empty list when nothing applies.
        /// </summary>
        private List<CustomUserGroup> resolveCustomGroups(IUser user)
        {
            List<CustomUserGroup> result = new();
            bool foundExplicit = false;

            lock (lockObject)
            {
                if (sessionOverrides.TryGetValue(user.OnlineID, out var sessionIds))
                {
                    foreach (var id in sessionIds)
                    {
                        if (findGroup(id) is { } g && !result.Contains(g)) result.Add(g);
                    }
                    if (result.Count > 0) foundExplicit = true;
                }

                if (!foundExplicit && persistedOverrides.TryGetValue(user.OnlineID, out var persistedIds))
                {
                    foreach (var id in persistedIds)
                    {
                        if (findGroup(id) is { } g && !result.Contains(g)) result.Add(g);
                    }
                    if (result.Count > 0) foundExplicit = true;
                }

                if (!enabled.Value || foundExplicit)
                    return result;

                var matchingRules = new List<GroupRule>();
                foreach (var rule in rules)
                {
                    if (matches(rule.Predicate, user))
                        matchingRules.Add(rule);
                }

                foreach (var rule in matchingRules.OrderByDescending(r => r.Priority).ThenByDescending(r => r.Id))
                {
                    if (findGroup(rule.GroupId) is { } g && !result.Contains(g))
                        result.Add(g);
                }
            }

            return result;
        }

        private static bool matches(Func<IUser, bool> predicate, IUser user)
        {
            try
            {
                return predicate(user);
            }
            catch
            {
                return false;
            }
        }

        private CustomUserGroup? findGroup(int groupId)
        {
            if (groupId <= 0)
                return null;

            lock (lockObject)
            {
                foreach (var group in persistedGroups)
                {
                    if (group.Id == groupId)
                        return group;
                }

                foreach (var registration in registeredGroups)
                {
                    if (registration.Group.Id == groupId)
                        return registration.Group;
                }
            }

            return null;
        }

        /// <summary>The real groups of a user, captured before any stamping; falls back to the live field for unstamped users.</summary>
        private APIUserGroup[]? realGroupsOf(IUser user)
        {
            if (user is not APIUser apiUser)
                return null;

            return realStates.TryGetValue(apiUser, out var real) ? real.Groups : apiUser.Groups;
        }

        /// <summary>
        /// Normalizes a user to its effective groups (real + resolved custom) and colour. The real
        /// state is captured on the first stamp of each <c>APIUser</c> object, so removing an
        /// override or disabling the feature can restore it. Returns <c>true</c> when the user's
        /// fields actually changed.
        /// </summary>
        private bool stamp(APIUser user)
        {
            var real = realStates.GetValue(user, static u => new RealUserState(u.Groups, u.Colour));

            var custom = resolveCustomGroups(user);

            APIUserGroup[]? groups = real.Groups;
            if (custom.Count > 0)
            {
                var list = real.Groups == null ? new List<APIUserGroup>() : real.Groups.ToList();
                foreach (var c in custom)
                {
                    list.RemoveAll(g => g.Id == c.Id);
                    list.Add(c.ToAPIUserGroup());
                }
                groups = list.ToArray();
            }

            string? colour = real.Colour;
            if (applyUsernameColour.Value)
                colour = custom.FirstOrDefault(g => !string.IsNullOrEmpty(g.Colour))?.Colour ?? real.Colour;

            bool changed = false;

            if (groups != null && user.Groups != null && groups.Length == user.Groups.Length)
            {
                for (int i = 0; i < groups.Length; i++)
                {
                    var a = groups[i];
                    var b = user.Groups[i];
                    if (a.Id != b.Id || a.Identifier != b.Identifier || a.Name != b.Name || a.ShortName != b.ShortName || a.Colour != b.Colour)
                    {
                        changed = true;
                        break;
                    }
                }
            }
            else if (groups != user.Groups)
            {
                changed = true;
            }

            if (user.Colour != colour)
                changed = true;

            if (changed)
            {
                user.Groups = groups;
                user.Colour = colour;
            }

            return changed;
        }

        /// <summary>The real groups/colour of an <c>APIUser</c>, captured before any stamping.</summary>
        private sealed record RealUserState(APIUserGroup[]? Groups, string? Colour);

        private readonly ConditionalWeakTable<APIUser, RealUserState> realStates = new();

        /// <summary>
        /// Live-updates the open profile page. <see cref="UserProfileOverlay"/> caches the shown
        /// user data and short-circuits re-fetching the same user, so its group badges would
        /// otherwise stay stale; re-stamps the cached copy in place and re-raises the header
        /// bindable, which propagates to every bound container.
        /// </summary>
        private void applyOpenProfile()
        {
            var game = ClientApi.Game;
            if (game == null)
                return;

            var overlay = Reflection.FindField(game.GetType(), "userProfile")?.GetValue(game) as UserProfileOverlay;
            if (overlay == null)
                return;

            var data = overlay.Header.User.Value;
            if (data?.User is not { } profileUser)
                return;

            if (!stamp(profileUser))
                return;

            triggerPropagated(overlay.Header.User);
        }

        /// <summary>
        /// Re-raises a bindable with propagation to every bound copy and without the same-value
        /// short-circuit, so UI bound to <c>api.LocalUser</c> or a profile header renders the
        /// just-stamped fields. <see cref="IBindable{T}.TriggerChange"/> cannot: it calls
        /// <c>TriggerValueChange</c> with <c>propagateToBindings:false</c>.
        /// </summary>
        private static void triggerPropagated<T>(IBindable<T> bindable)
        {
            try
            {
                var trigger = triggerCache.GetOrAdd(bindable.GetType(), static t =>
                    t.GetMethod("TriggerValueChange", BindingFlags.Instance | BindingFlags.NonPublic));
                trigger?.Invoke(bindable, new object[] { bindable.Value!, bindable, true, true });
            }
            catch (Exception ex)
            {
                TimingLog.Error($"triggerPropagated failed: {ex}");
            }
        }

        private static readonly ConcurrentDictionary<Type, MethodInfo?> triggerCache = new();

        private void walk(object node, HashSet<object> visited, int depth)
        {
            Type nodeType = node.GetType();

            if (depth > maxWalkDepth || nodeType.IsValueType || nodeType == typeof(string) || !visited.Add(node))
                return;

            if (node is APIUser user)
                stamp(user);

            if (depth == maxWalkDepth)
                return;

            MemberInfo[] members = memberCache.GetOrAdd(nodeType, static t =>
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
                return t.GetFields(flags)
                        .Cast<MemberInfo>()
                        .Concat(t.GetProperties(flags)
                                  .Where(p => p.GetIndexParameters().Length == 0 && p.GetMethod != null)
                                  .Cast<MemberInfo>())
                        .ToArray();
            });

            foreach (MemberInfo member in members)
            {
                object? value = readMember(node, member);
                if (value == null)
                    continue;

                Type valueType = value.GetType();
                if (valueType.IsValueType || valueType == typeof(string))
                    continue;

                if (value is IEnumerable enumerable)
                {
                    // Best-effort snapshot: the underlying collection may be mutated concurrently
                    // by another API request thread, which throws during enumeration; skip it then.
                    try
                    {
                        foreach (object? item in enumerable.Cast<object>().ToArray())
                        {
                            if (item != null)
                                walk(item, visited, depth + 1);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // collection changed while snapshotting; leave it unstamped (cosmetic)
                    }
                }
                else
                {
                    walk(value, visited, depth + 1);
                }
            }
        }

        private static readonly ConcurrentDictionary<Type, MemberInfo[]> memberCache = new();

        private static object? readMember(object node, MemberInfo member)
        {
            try
            {
                return member switch
                {
                    FieldInfo field => field.GetValue(node),
                    PropertyInfo property => property.GetValue(node),
                    _ => null,
                };
            }
            catch
            {
                return null;
            }
        }

        private void applyPersistedGroups()
        {
            lock (lockObject)
            {
                persistedGroups.Clear();

                List<CustomUserGroup>? parsed = null;

                try
                {
                    parsed = JsonSerializer.Deserialize<List<CustomUserGroup>>(groups.Value, jsonOptions);
                }
                catch (JsonException)
                {
                }

                if (parsed != null)
                {
                    foreach (var group in parsed)
                    {
                        if (group != null && group.Id > 0)
                            persistedGroups.Add(group);
                    }
                }
            }

            Changed?.Invoke();
        }

        private void applyPersistedOverrides()
        {
            lock (lockObject)
            {
                persistedOverrides.Clear();

                List<UserGroupOverride>? parsed = null;

                try
                {
                    parsed = JsonSerializer.Deserialize<List<UserGroupOverride>>(userOverrides.Value, jsonOptions);
                }
                catch (JsonException)
                {
                }

                persistedList.Clear();

                if (parsed != null)
                    persistedList.AddRange(parsed);

                foreach (var userOverride in persistedList)
                {
                    if (userOverride.UserId <= 0 || userOverride.GroupId <= 0)
                        continue;

                    if (!persistedOverrides.TryGetValue(userOverride.UserId, out var list))
                        persistedOverrides[userOverride.UserId] = list = new List<int>();
                    list.Add(userOverride.GroupId);
                }
            }

            Changed?.Invoke();
        }

        private void writePersistedOverrides()
        {
            string json;

            lock (lockObject)
                json = JsonSerializer.Serialize(persistedList, jsonOptions);

            userOverrides.Value = json;

            // Re-apply even when the serialized value round-trips to the same string (no ValueChanged).
            applyPersistedOverrides();
        }

        private readonly record struct GroupRule(long Id, Func<IUser, bool> Predicate, int GroupId, int Priority);

        private readonly record struct RegisteredGroup(long HandleId, CustomUserGroup Group);

        /// <summary>Removes its rule on dispose.</summary>
        private sealed class RuleHandle : IDisposable
        {
            private readonly CustomUserGroupsApi owner;
            private readonly long id;
            private bool disposed;

            public RuleHandle(CustomUserGroupsApi owner, long id)
            {
                this.owner = owner;
                this.id = id;
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                owner.removeRule(id);
            }
        }

        /// <summary>Unregisters its group on dispose.</summary>
        private sealed class GroupRegistrationHandle : IDisposable
        {
            private readonly CustomUserGroupsApi owner;
            private readonly long handleId;
            private bool disposed;

            public GroupRegistrationHandle(CustomUserGroupsApi owner, long handleId)
            {
                this.owner = owner;
                this.handleId = handleId;
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                owner.unregisterGroup(handleId);
            }
        }
    }
}
