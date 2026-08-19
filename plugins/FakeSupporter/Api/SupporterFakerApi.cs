using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Login;
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

namespace FakeSupporter
{
    /// <summary>How a per-user supporter override forces the user's supporter state.</summary>
    public enum SupporterOverrideMode
    {
        /// <summary>Force the user to be shown with a supporter tag.</summary>
        ForceSupporter = 0,

        /// <summary>Force the user to be shown without a supporter tag (regardless of the real state).</summary>
        ForceNotSupporter = 1,
    }

    /// <summary>A per-user supporter override persisted in the plugin's <c>user_overrides</c> setting.</summary>
    public sealed class SupporterUserOverride
    {
        /// <summary>The osu! user id this override applies to.</summary>
        public int UserId { get; set; }

        /// <summary>How this override forces the user's supporter state.</summary>
        public SupporterOverrideMode Mode { get; set; } = SupporterOverrideMode.ForceSupporter;

        /// <summary>The faked supporter level (1–10 hearts); <c>null</c> falls back to the plugin's level setting.</summary>
        public int? Level { get; set; }
    }

    /// <summary>
    /// Concrete <see cref="ISupporterFakerApi"/>: holds the plugin's master toggle and level, the
    /// per-user overrides (session + persisted) and the rules registered by other plugins, and
    /// resolves the effective supporter state for a user. The plugin exports this instance under
    /// the <c>fake-supporter</c> id and its patches stamp every API response through it.
    /// </summary>
    public sealed class SupporterFakerApi : ISupporterFakerApi
    {
        private const string enabledKey = "enabled";
        private const string levelKey = "level";
        private const string userOverridesKey = "user_overrides";
        private const int maxWalkDepth = 12;

        /// <summary>The singleton the plugin exports and its patches resolve through.</summary>
        public static SupporterFakerApi Instance { get; internal set; } = null!;

        /// <summary>The plugin host, set by <see cref="Attach"/> so the API can log into its own file.</summary>
        private static IOsuCcPluginHost host = null!;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly object lockObject = new();

        private readonly List<Rule> rules = new();
        private readonly Dictionary<int, PerUserEntry> persistedOverrides = new();
        private readonly Dictionary<int, PerUserEntry> sessionOverrides = new();
        private readonly List<SupporterUserOverride> persistedList = new();

        private long nextRuleId;

        private Bindable<bool> enabled = new(false);
        private Bindable<int> level = new(2);
        private Bindable<string> userOverrides = new(string.Empty);

        // Read from background request threads and written from the update thread; plain int/bool
        // reads are atomic, and the worst a race can produce is a response stamped one tick late.
        private int localId;
        private Bindable<APIUser>? localUser;

        // The toolbar avatar button's LoginOverlay, captured from its load method (production
        // builds no longer expose it as an OsuGame field; it lives on the main-menu screens).
        private LoginOverlay? loginOverlay;

        // Weakly-keyed trackers for the mini user cards that need their layout rebuilt on change.
        private readonly ConditionalWeakTable<UserPanel, PanelTracker> panelTrackers = new();

        /// <inheritdoc />
        public bool Enabled => enabled.Value;

        /// <inheritdoc />
        public int Level => Math.Clamp(level.Value, 1, 10);

        /// <inheritdoc />
        public event Action? Changed;

        /// <summary>Wires the API to the plugin's persisted settings. Called once during <see cref="IOsuCcPlugin.Load"/>.</summary>
        public void Attach(PluginSettings settings, IOsuCcPluginHost host)
        {
            SupporterFakerApi.host = host;
            enabled = settings.Bind(enabledKey, false);
            level = settings.Bind(levelKey, 2);
            userOverrides = settings.Bind(userOverridesKey, string.Empty);

            enabled.ValueChanged += _ => onEnabledChanged();
            level.ValueChanged += _ => onLevelChanged();
            userOverrides.ValueChanged += _ => applyPersistedOverrides();

            applyPersistedOverrides();
        }

        /// <summary>The currently persisted per-user overrides (settings-driven), for display.</summary>
        public IReadOnlyList<SupporterUserOverride> PersistedOverrides
        {
            get
            {
                lock (lockObject)
                    return persistedList.ToArray();
            }
        }

        /// <summary>Adds or updates a persisted per-user override, writing it to the plugin settings.</summary>
        public void SetPersistedOverride(SupporterUserOverride userOverride)
        {
            if (userOverride == null || userOverride.UserId <= 0)
                return;

            lock (lockObject)
            {
                persistedList.RemoveAll(o => o.UserId == userOverride.UserId);
                persistedList.Add(userOverride);
            }

            writePersistedOverrides();
        }

        /// <summary>Removes a persisted per-user override, writing the change to the plugin settings.</summary>
        public void RemovePersistedOverride(int userId)
        {
            lock (lockObject)
            {
                if (persistedList.RemoveAll(o => o.UserId == userId) == 0)
                    return;
            }

            writePersistedOverrides();
        }

        public IDisposable AddRule(Func<IUser, bool> predicate, bool isSupporter, int? level, int priority = 0)
        {
            ArgumentNullException.ThrowIfNull(predicate);

            long id;

            lock (lockObject)
            {
                id = ++nextRuleId;
                rules.Add(new Rule(id, predicate, isSupporter, level, priority));
            }

            Changed?.Invoke();
            return new RuleHandle(this, id);
        }

        public void SetUserSupporter(int userId, SupporterOverrideMode mode, int? level)
        {
            if (userId <= 0)
                return;

            lock (lockObject)
                sessionOverrides[userId] = new PerUserEntry
                {
                    Mode = mode,
                    Level = mode == SupporterOverrideMode.ForceSupporter ? level : null,
                };

            Changed?.Invoke();
        }

        public void ClearUserSupporter(int userId)
        {
            lock (lockObject)
            {
                if (!sessionOverrides.Remove(userId))
                    return;
            }

            Changed?.Invoke();
        }

        public bool? ResolveIsSupporter(IUser user)
        {
            if (user == null)
                return null;

            if (!TryResolve(user, out bool isSupporter, out _))
                return null;

            return isSupporter;
        }

        public int ResolveLevel(IUser user)
        {
            if (user == null)
                return 0;

            if (!TryResolve(user, out bool isSupporter, out int resolvedLevel))
                return 0;

            return isSupporter ? resolvedLevel : 0;
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

        /// <summary>Captures the toolbar avatar button's login overlay so its mini card can be rebuilt later.</summary>
        public void SetLoginOverlay(LoginOverlay? overlay)
        {
            loginOverlay = overlay;
            host.Log(LogLevel.Info, $"login overlay {(overlay == null ? "unavailable" : "captured")}");
        }

        /// <summary>
        /// Tracks a <see cref="UserPanel"/> (mini user card: friend list, online players, chat
        /// users, …) so its supporter tag can be rebuilt when the effective state changes. Called
        /// from the <c>UserPanel.load</c> postfix; panels build their layout once and never redraw
        /// it, so without this hook a per-user override applied later would not reach cards that
        /// were created before the change.
        /// </summary>
        public void OnPanelCreated(UserPanel panel)
        {
            if (panel == null)
                return;

            try
            {
                panelTrackers.GetValue(panel, static p => new PanelTracker(p));

                // Normalize a panel created from a previously-faked user (e.g. a cached friend list
                // object stamped while an override was active). No-op when the user is already correct.
                deferRefresh(panel);
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"failed to track mini panel {panel.GetType().Name}: {ex}");
            }
        }

        /// <summary>
        /// Stamps every user inside a freshly deserialized API response. Called from the
        /// <c>APIRequest.Perform</c> postfix, which covers all /api/v2 JSON responses. The /me
        /// response (<see cref="APIMe"/>) is skipped here — it is handled by <see cref="OnLocalUserSet"/>
        /// so the game's own config write keeps the real value.
        /// </summary>
        public void ApplyToResponse(object? root)
        {
            if (root == null || root is APIMe || !HasActiveState)
                return;

            walk(root, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
        }

        private bool HasActiveState => enabled.Value || rules.Count > 0 || sessionOverrides.Count > 0 || persistedOverrides.Count > 0;

        private void removeRule(long id)
        {
            lock (lockObject)
                rules.RemoveAll(r => r.Id == id);

            Changed?.Invoke();
        }

        private void onEnabledChanged()
        {
            applyLocalUser();

            Changed?.Invoke();
        }

        private void onLevelChanged()
        {
            if (enabled.Value)
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
            refreshLoginPanel();
        }

        /// <summary>
        /// Resolves the effective supporter state for a user: a session override, then a persisted
        /// override, then the current player (only while the master toggle is on), then the best
        /// matching rule by priority. When nothing matches, the real state is left untouched.
        /// </summary>
        private bool TryResolve(IUser user, out bool isSupporter, out int resolvedLevel)
        {
            isSupporter = false;
            resolvedLevel = 0;

            lock (lockObject)
            {
                if (sessionOverrides.TryGetValue(user.OnlineID, out var session))
                    return applyDecision(session.Mode, session.Level, out isSupporter, out resolvedLevel);

                if (persistedOverrides.TryGetValue(user.OnlineID, out var persisted))
                    return applyDecision(persisted.Mode, persisted.Level, out isSupporter, out resolvedLevel);
            }

            if (user.OnlineID == localId && enabled.Value)
            {
                isSupporter = true;
                resolvedLevel = Level;
                return true;
            }

            Rule? best = null;

            lock (lockObject)
            {
                foreach (var rule in rules)
                {
                    if (!matches(rule.Predicate, user))
                        continue;

                    if (best == null || rule.Priority > best.Value.Priority || (rule.Priority == best.Value.Priority && rule.Id > best.Value.Id))
                        best = rule;
                }
            }

            if (best != null)
                return applyDecision(best.Value.IsSupporter ? SupporterOverrideMode.ForceSupporter : SupporterOverrideMode.ForceNotSupporter, best.Value.Level, out isSupporter, out resolvedLevel);

            return false;
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

        private bool applyDecision(SupporterOverrideMode mode, int? requestedLevel, out bool supporter, out int resolvedLevel)
        {
            supporter = mode == SupporterOverrideMode.ForceSupporter;
            resolvedLevel = supporter ? Math.Clamp(requestedLevel ?? Level, 1, 10) : 0;
            return true;
        }

        /// <summary>
        /// Normalizes a user to either the effective faked state (override/rule/player toggle) or,
        /// when nothing applies, back to the user's real supporter state. The real state is captured
        /// on the first stamp of each <c>APIUser</c> object, so removing an override or disabling the
        /// feature can restore it. Returns <c>true</c> when the user's fields actually changed.
        /// </summary>
        private bool stamp(APIUser user)
        {
            var real = realStates.GetValue(user, static u => new RealSupporterState(u.IsSupporter, u.SupportLevel));

            bool isSupporter;
            int resolvedLevel;

            if (TryResolve(user, out isSupporter, out resolvedLevel))
            {
                bool changed = user.IsSupporter != isSupporter || user.SupportLevel != resolvedLevel;
                user.IsSupporter = isSupporter;
                user.SupportLevel = resolvedLevel;
                return changed;
            }

            // Nothing applies — restore the user's real state (a previous stamp may have faked it).
            bool reverted = user.IsSupporter != real.IsSupporter || user.SupportLevel != real.SupportLevel;
            user.IsSupporter = real.IsSupporter;
            user.SupportLevel = real.SupportLevel;
            return reverted;
        }

        /// <summary>The original supporter state of a <c>APIUser</c>, captured before any faking.</summary>
        private sealed record RealSupporterState(bool IsSupporter, int SupportLevel);

        private readonly ConditionalWeakTable<APIUser, RealSupporterState> realStates = new();

        /// <summary>
        /// Rebuilds the mini user card of the toolbar login overlay in place. The card is a
        /// <see cref="UserRankPanel"/> snapshot taken once when the API went online, so it keeps
        /// its original supporter level until it is rebuilt; swap it for a fresh one while keeping
        /// the sibling <see cref="UserDropdown"/> (disposed by the <c>Children</c> setter).
        /// </summary>
        private void refreshLoginPanel()
        {
            var game = ClientApi.Game;
            if (game == null)
                return;

            // The overlay instance comes from the toolbar button capture (ownership moved to the
            // main-menu screens in production); the game's dependency container is a fallback.
            var overlay = loginOverlay ?? game.Dependencies?.Get(typeof(LoginOverlay)) as LoginOverlay;

            if (overlay == null)
            {
                host.Log(LogLevel.Info, "login overlay unavailable (not captured)");
                return;
            }

            var panel = Reflection.FindField(overlay.GetType(), "panel")?.GetValue(overlay) as LoginPanel;
            if (panel == null || panel.Children.Count != 1 || panel.Children[0] is not FillFlowContainer flow)
            {
                // The `Child` getter throws on an empty container, so use the children list; the
                // online flow may not be built yet right after SetLocalUser (Connecting -> Online).
                host.Log(LogLevel.Info, "login panel not ready for mini-card swap");
                return;
            }

            var oldPanel = flow.Children.OfType<UserRankPanel>().FirstOrDefault();
            if (oldPanel == null)
            {
                host.Log(LogLevel.Info, "login panel has no user card to swap");
                return;
            }

            var user = localUser?.Value;
            if (user == null)
                return;

            var newPanel = new UserRankPanel(user)
            {
                RelativeSizeAxes = Axes.X,
                Action = oldPanel.Action,
            };

            var dropdown = flow.Children.OfType<UserDropdown>().FirstOrDefault();

            flow.Remove(oldPanel, true);

            // Keep the sibling dropdown alive across the swap (the Children setter would dispose it).
            if (dropdown != null)
                flow.Remove(dropdown, false);

            flow.Add(newPanel);

            if (dropdown != null)
                flow.Add(dropdown);

            host.Log(LogLevel.Info, $"login panel mini-card swapped (level={Level})");
        }

        /// <summary>Re-stamps a tracked mini panel's user and rebuilds its layout when anything changed.</summary>
        private void refreshPanel(UserPanel panel)
        {
            try
            {
                if (isDisposed(panel))
                    return;

                if (!stamp(panel.User))
                    return;

                rebuildLayout(panel);
                host.Log(LogLevel.Info, $"mini panel rebuilt ({panel.GetType().Name} userId={panel.User.OnlineID})");
            }
            catch (Exception ex)
            {
                host.Log(LogLevel.Error, $"failed to refresh mini panel {panel.GetType().Name}: {ex}");
            }
        }

        /// <summary>
        /// Runs <see cref="refreshPanel"/> on the game's update thread, coalesced per frame. Panel
        /// mutations must happen on the update thread, and <c>Changed</c> can fire from request
        /// threads or the settings UI.
        /// </summary>
        private void deferRefresh(UserPanel panel)
        {
            var game = ClientApi.Game;
            var scheduler = game == null ? null : Reflection.GetScheduler(game);

            if (scheduler != null)
                scheduler.AddOnce(() => refreshPanel(panel));
            else
                refreshPanel(panel);
        }

        private static readonly ConcurrentDictionary<Type, MethodInfo?> createLayoutCache = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo?> internalChildrenCache = new();

        private static readonly PropertyInfo isDisposedProperty = typeof(Drawable).GetProperty("IsDisposed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

        /// <summary><see cref="Drawable.IsDisposed"/> is protected-internal, so it is read reflectively.</summary>
        private static bool isDisposed(Drawable? drawable)
            => drawable != null && (isDisposedProperty?.GetValue(drawable) as bool? ?? false);

        /// <summary>
        /// Swaps a panel's layout for a fresh one. <c>UserPanel.load()</c> adds the
        /// <c>CreateLayout()</c> result last, and the internal children sort is stable by
        /// <c>ChildID</c>, so the layout is the final <c>InternalChildren</c> entry. Only public
        /// members are used — <c>InternalChildren</c> is protected-internal, so it is read
        /// reflectively; <c>CreateLayout()</c> is protected, so it is invoked reflectively.
        /// </summary>
        private static void rebuildLayout(UserPanel panel)
        {
            var createLayout = createLayoutCache.GetOrAdd(panel.GetType(), static t =>
                t.GetMethod("CreateLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

            if (createLayout == null || createLayout.Invoke(panel, null) is not Drawable newLayout)
                return;

            var childrenProperty = internalChildrenCache.GetOrAdd(panel.GetType(), static _ =>
                typeof(CompositeDrawable).GetProperty("InternalChildren", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

            if (childrenProperty?.GetValue(panel) is not IReadOnlyList<Drawable> children || children.Count == 0)
                return;

            panel.Remove(children[children.Count - 1], true);
            panel.Add(newLayout);
        }

        /// <summary>
        /// Keeps a weak reference to one mini panel and refreshes it whenever the API state
        /// changes. Entries die with their panel: the panel is only held weakly, and on the next
        /// change a dead panel's tracker unsubscribes itself.
        /// </summary>
        private sealed class PanelTracker
        {
            private readonly WeakReference<UserPanel> panelRef;

            public PanelTracker(UserPanel panel)
            {
                panelRef = new WeakReference<UserPanel>(panel);
                Instance.Changed += refresh;
            }

            private void refresh()
            {
                if (!panelRef.TryGetTarget(out var panel) || isDisposed(panel))
                {
                    Instance.Changed -= refresh;
                    return;
                }

                Instance.deferRefresh(panel);
            }
        }

        /// <summary>
        /// Live-updates the open profile page. <see cref="UserProfileOverlay"/> caches the shown
        /// <see cref="osu.Game.Overlays.Profile.UserProfileData"/> and short-circuits re-fetching
        /// the same user, so its hearts would otherwise stay stale; re-stamps the cached copy in
        /// place and re-raises the header bindable, which propagates to every bound container.
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
                host.Log(LogLevel.Error, $"failed to re-raise user bindable: {ex}");
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

        private readonly record struct Rule(long Id, Func<IUser, bool> Predicate, bool IsSupporter, int? Level, int Priority);

        private sealed class PerUserEntry
        {
            public SupporterOverrideMode Mode;
            public int? Level;
        }

        private void applyPersistedOverrides()
        {
            lock (lockObject)
            {
                persistedOverrides.Clear();

                List<SupporterUserOverride>? parsed = null;

                try
                {
                    parsed = JsonSerializer.Deserialize<List<SupporterUserOverride>>(userOverrides.Value, jsonOptions);
                }
                catch (JsonException)
                {
                }

                persistedList.Clear();

                if (parsed != null)
                    persistedList.AddRange(parsed);

                foreach (var userOverride in persistedList)
                {
                    if (userOverride.UserId <= 0)
                        continue;

                    persistedOverrides[userOverride.UserId] = new PerUserEntry
                    {
                        Mode = userOverride.Mode,
                        Level = userOverride.Mode == SupporterOverrideMode.ForceSupporter ? userOverride.Level : null,
                    };
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

        /// <summary>Removes its rule on dispose.</summary>
        private sealed class RuleHandle : IDisposable
        {
            private readonly SupporterFakerApi owner;
            private readonly long id;
            private bool disposed;

            public RuleHandle(SupporterFakerApi owner, long id)
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
    }
}
