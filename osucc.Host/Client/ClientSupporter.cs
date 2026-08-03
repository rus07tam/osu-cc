using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Login;
using osu.Game.Users;
using osucc.Core;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace osucc.Client
{
    /// <summary>
    /// Locally fakes the current player's osu!supporter tag. Purely cosmetic: nothing is sent to
    /// the servers, only freshly deserialized <see cref="APIUser"/> copies are stamped in memory.
    /// The /me response is stamped by the <c>SetLocalUser</c> patch (after the game wrote its own
    /// WasSupporter config, which therefore keeps the real value); every other API response is
    /// stamped by the <c>APIRequest.Perform</c> postfix, which walks the response graph for copies
    /// of the current user.
    /// </summary>
    public static class ClientSupporter
    {
        private const int maxWalkDepth = 12;

        // Read from background request threads and written from the update thread; plain int/bool
        // reads are atomic, and the worst a race can produce is a response stamped one tick late.
        private static bool enabled;
        private static int level = 2;
        private static int localId;
        private static Bindable<APIUser>? localUser;

        private static Bindable<bool>? enabledBindable;
        private static Bindable<int>? levelBindable;
        private static bool attached;

        // The toolbar avatar button's LoginOverlay, captured from its load method (production
        // builds no longer expose it as an OsuGame field; it lives on the main-menu screens).
        private static LoginOverlay? loginOverlay;

        public static void SetLoginOverlay(LoginOverlay? overlay)
        {
            loginOverlay = overlay;
            TimingLog.Info($"ToolbarUserButton: login overlay {(overlay == null ? "unavailable" : "captured")}");
        }

        public static void Attach(SpecialsConfigManager config)
        {
            if (attached)
                return;

            // Strong refs: ConfigManager.GetBindable returns weak copies, so the subscriptions
            // below would die after the first (immediate) fire otherwise.
            enabledBindable = config.GetBindable<bool>(SpecialsSetting.FakeSupporterEnabled);
            levelBindable = config.GetBindable<int>(SpecialsSetting.FakeSupporterLevel);
            enabledBindable.BindValueChanged(e => onEnabledChanged(e.NewValue), true);
            levelBindable.BindValueChanged(e => onLevelChanged(e.NewValue), true);

            attached = true;
            TimingLog.Info($"ClientSupporter attached (enabled={enabled}, level={level})");
        }

        private static void onEnabledChanged(bool newValue)
        {
            enabled = newValue;

            // Re-stamp the live local user right away so the toggle works without a restart.
            if (enabled)
                applyLocalUser();

            TimingLog.Info($"Fake supporter enabled={newValue}");
        }

        private static void onLevelChanged(int newValue)
        {
            level = Math.Clamp(newValue, 1, 10);

            if (enabled)
                applyLocalUser();

            TimingLog.Info($"Fake supporter level={level}");
        }

        /// <summary>
        /// Called from the <c>LocalUserState.SetLocalUser</c> postfix once the real /me response is
        /// installed (update thread). Stamps the local user and re-raises its bindable so existing
        /// UI re-renders; the game's WasSupporter config already holds the real value.
        /// </summary>
        public static void OnLocalUserSet(IBindable<APIUser> user)
        {
            APIUser me = user.Value;
            if (me == null)
                return;

            localId = me.Id;
            localUser = user as Bindable<APIUser>;

            if (enabled)
            {
                stamp(me);
                triggerPropagated(user);
                applyOpenProfile();
                refreshLoginPanel();
            }
        }

        /// <summary>Called from the <c>LocalUserState.ClearLocalUser</c> postfix (logout): forget the cached user.</summary>
        public static void OnLocalUserCleared()
        {
            localId = 0;
            localUser = null;
        }

        /// <summary>
        /// Stamps every copy of the current user inside a freshly deserialized API response. Called
        /// from the <c>APIRequest.Perform</c> postfix, which covers all /api/v2 JSON responses. The
        /// /me response (<see cref="APIMe"/>) is skipped here — it is handled by
        /// <see cref="OnLocalUserSet"/> so the game's own config write keeps the real value.
        /// </summary>
        public static void ApplyToResponse(object? root)
        {
            if (root == null || root is APIMe || !enabled || localId <= 0)
                return;

            walk(root, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
        }

        private static void applyLocalUser()
        {
            var user = localUser?.Value;
            if (user == null)
                return;

            stamp(user);
            triggerPropagated(localUser!);
            applyOpenProfile();
            refreshLoginPanel();
        }

        /// <summary>
        /// Rebuilds the mini user card of the toolbar login overlay in place. The card is a
        /// <see cref="UserRankPanel"/> snapshot taken once when the API went online, so it keeps
        /// its original supporter level until it is rebuilt; swap it for a fresh one while keeping
        /// the sibling <see cref="UserDropdown"/> (disposed by the <c>Children</c> setter).
        /// </summary>
        private static void refreshLoginPanel()
        {
            var game = ClientApi.Game;
            if (game == null)
                return;

            // The overlay instance comes from the toolbar button capture (ownership moved to the
            // main-menu screens in production); the game's dependency container is a fallback.
            var overlay = loginOverlay ?? game.Dependencies?.Get(typeof(LoginOverlay)) as LoginOverlay;

            if (overlay == null)
            {
                TimingLog.Info("Login overlay unavailable (not captured)");
                return;
            }

            var panel = Reflection.FindField(overlay.GetType(), "panel")?.GetValue(overlay) as LoginPanel;
            if (panel == null || panel.Children.Count != 1 || panel.Children[0] is not FillFlowContainer flow)
            {
                // The `Child` getter throws on an empty container, so use the children list; the
                // online flow may not be built yet right after SetLocalUser (Connecting -> Online).
                TimingLog.Info("Login panel not ready for mini-card swap");
                return;
            }

            var oldPanel = flow.Children.OfType<UserRankPanel>().FirstOrDefault();
            if (oldPanel == null)
            {
                TimingLog.Info("Login panel has no user card to swap");
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

            TimingLog.Info($"Login panel mini-card swapped (level={level})");
        }

        /// <summary>
        /// Live-updates the open profile page. <see cref="UserProfileOverlay"/> caches the shown
        /// <see cref="osu.Game.Overlays.Profile.UserProfileData"/> and short-circuits re-fetching
        /// the same user, so its hearts would otherwise stay stale; re-stamps the cached copy in
        /// place and re-raises the header bindable, which propagates to every bound container.
        /// </summary>
        private static void applyOpenProfile()
        {
            var game = ClientApi.Game;
            if (game == null)
                return;

            var overlay = Reflection.FindField(game.GetType(), "userProfile")?.GetValue(game) as UserProfileOverlay;
            if (overlay == null)
                return;

            var data = overlay.Header.User.Value;
            if (data?.User is not { } profileUser || profileUser.Id != localId)
                return;

            stamp(profileUser);
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

        private static void stamp(APIUser user)
        {
            user.IsSupporter = true;
            user.SupportLevel = level;
        }

        private static readonly ConcurrentDictionary<Type, MemberInfo[]> memberCache = new();

        private static void walk(object node, HashSet<object> visited, int depth)
        {
            Type nodeType = node.GetType();

            if (depth > maxWalkDepth || nodeType.IsValueType || nodeType == typeof(string) || !visited.Add(node))
                return;

            if (node is APIUser user && user.Id == localId)
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
    }
}
