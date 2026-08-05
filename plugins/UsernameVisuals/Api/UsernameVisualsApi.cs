using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Users;
using osucc.Plugin;
using System.Text.Json;

namespace UsernameVisuals
{
    /// <summary>A per-user colour/display override persisted in the plugin's <c>user_overrides</c> setting.</summary>
    public sealed class UsernameUserOverride
    {
        /// <summary>The osu! user id this override applies to.</summary>
        public int UserId { get; set; }

        /// <summary>Comma-separated hex gradient colours; empty or <c>null</c> leaves the colour untouched.</summary>
        public string? Palette { get; set; }

        /// <summary>Display name replacing the real one; empty or <c>null</c> leaves the name untouched.</summary>
        public string? Name { get; set; }

        /// <summary>Whether the username should be hidden behind a solid block (takes precedence over <see cref="Name"/>).</summary>
        public bool Hide { get; set; }
    }

    /// <summary>
    /// Concrete <see cref="IUsernameVisualsApi"/>: holds the plugin's settings-driven base
    /// conditionals (own-username colour, fallback colour for everyone else, own-username
    /// display override) alongside conditionals registered by other plugins and per-user
    /// overrides, and resolves the effective palette / display rule for a user.
    /// The plugin exports this instance under the <c>username-visuals</c> id and its own
    /// username texts resolve through it.
    /// </summary>
    public sealed class UsernameVisualsApi : IUsernameVisualsApi
    {
        private const string gradientEnabledKey = "gradient_enabled";
        private const string selfPaletteKey = "self_palette";
        private const string othersPaletteKey = "others_palette";
        private const string replaceEnabledKey = "own_replace_enabled";
        private const string replaceNameKey = "own_replace_name";
        private const string hideEnabledKey = "own_hide_enabled";
        private const string userOverridesKey = "user_overrides";

        private const int selfPriority = 0;
        private const int fallbackPriority = -1;

        // The user's own display settings (hide, replace) must always beat rules registered by other
        // plugins, so their base rule uses a priority beyond anything a plugin is expected to use.
        private const int userPreferencePriority = 1_000_000;

        /// <summary>The singleton the plugin exports and its own username texts resolve through.</summary>
        public static UsernameVisualsApi? Instance { get; internal set; }

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly object lockObject = new();

        private readonly List<ColourRule> colourRules = new();
        private readonly List<NameRule> nameRules = new();
        private readonly List<IDisposable> baseRuleHandles = new();

        // Per-user overrides: persisted ones come from the plugin settings, session ones are
        // set at runtime via the API. Session overrides win over persisted ones.
        private readonly Dictionary<int, PerUserEntry> persistedOverrides = new();
        private readonly Dictionary<int, PerUserEntry> sessionOverrides = new();
        private readonly List<UsernameUserOverride> persistedList = new();

        private long nextRuleId;

        private Bindable<bool> gradientEnabled = new(false);
        private Bindable<string> selfPalette = new(string.Empty);
        private Bindable<string> othersPalette = new(string.Empty);
        private Bindable<bool> replaceEnabled = new(false);
        private Bindable<string> replaceName = new(string.Empty);
        private Bindable<bool> hideEnabled = new(false);
        private Bindable<string> userOverrides = new(string.Empty);

        /// <summary>Whether gradient rendering is enabled at all.</summary>
        public bool Enabled => gradientEnabled.Value;

        /// <summary>Fired whenever any rule, override or setting changes.</summary>
        public event Action? Changed;

        /// <summary>
        /// Wires the API to the plugin's persisted settings and registers the base conditionals.
        /// Called once during <see cref="osucc.Plugin.IOsuCcPlugin.Load"/>.
        /// </summary>
        public void Attach(PluginSettings settings)
        {
            gradientEnabled = settings.Bind(gradientEnabledKey, false);
            selfPalette = settings.Bind(selfPaletteKey, string.Empty);
            othersPalette = settings.Bind(othersPaletteKey, string.Empty);
            replaceEnabled = settings.Bind(replaceEnabledKey, false);
            replaceName = settings.Bind(replaceNameKey, string.Empty);
            hideEnabled = settings.Bind(hideEnabledKey, false);
            userOverrides = settings.Bind(userOverridesKey, string.Empty);

            gradientEnabled.ValueChanged += _ => onSettingsChanged();
            selfPalette.ValueChanged += _ => onSettingsChanged();
            othersPalette.ValueChanged += _ => onSettingsChanged();
            replaceEnabled.ValueChanged += _ => onSettingsChanged();
            replaceName.ValueChanged += _ => onSettingsChanged();
            hideEnabled.ValueChanged += _ => onSettingsChanged();
            userOverrides.ValueChanged += _ => applyPersistedOverrides();

            onSettingsChanged();
            applyPersistedOverrides();
        }

        /// <summary>The currently persisted per-user overrides (settings-driven), for display.</summary>
        public IReadOnlyList<UsernameUserOverride> PersistedOverrides
        {
            get
            {
                lock (lockObject)
                    return persistedList.ToArray();
            }
        }

        /// <summary>Adds or updates a persisted per-user override, writing it to the plugin settings.</summary>
        public void SetPersistedOverride(UsernameUserOverride userOverride)
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

        public IDisposable AddColourRule(Func<IUsernameVisualsContext, bool> predicate, IReadOnlyList<Colour4> palette, int priority = 0)
        {
            var handle = addColourRuleInternal(predicate, palette, priority);
            Changed?.Invoke();
            return handle;
        }

        public IDisposable AddNameRule(Func<IUsernameVisualsContext, bool> predicate, UsernameNameRule rule, int priority = 0)
        {
            var handle = addNameRuleInternal(predicate, rule, priority);
            Changed?.Invoke();
            return handle;
        }

        public void SetUserColour(int userId, IReadOnlyList<Colour4> palette)
        {
            if (userId <= 0)
                return;

            lock (lockObject)
            {
                var entry = getOrCreate(sessionOverrides, userId);
                entry.Palette = palette.ToArray();
            }

            Changed?.Invoke();
        }

        public void ClearUserColour(int userId)
        {
            lock (lockObject)
            {
                if (!sessionOverrides.TryGetValue(userId, out var entry) || entry.Palette == null)
                    return;

                entry.Palette = null;
                prune(sessionOverrides, userId);
            }

            Changed?.Invoke();
        }

        public void SetUserName(int userId, UsernameNameRule rule)
        {
            if (userId <= 0)
                return;

            lock (lockObject)
            {
                var entry = getOrCreate(sessionOverrides, userId);
                entry.NameRule = rule.Mode == UsernameNameMode.Normal ? null : rule;
            }

            Changed?.Invoke();
        }

        public void ClearUserName(int userId)
        {
            lock (lockObject)
            {
                if (!sessionOverrides.TryGetValue(userId, out var entry) || entry.NameRule == null)
                    return;

                entry.NameRule = null;
                prune(sessionOverrides, userId);
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// Resolves the palette for a user, or <c>null</c> if gradient rendering should not apply
        /// (disabled, no user, or an empty palette). Per-user overrides win, then the best matching
        /// colour rule by priority, then nothing.
        /// </summary>
        public IReadOnlyList<Colour4>? ResolveColour(IUser? user, APIUser? localUser)
        {
            if (!Enabled || user == null)
                return null;

            var context = new Context(user, localUser);
            Colour4[]? palette;

            lock (lockObject)
            {
                if (sessionOverrides.TryGetValue(user.OnlineID, out var session) && session.Palette is { Length: > 0 })
                    palette = session.Palette;
                else if (persistedOverrides.TryGetValue(user.OnlineID, out var persisted) && persisted.Palette is { Length: > 0 })
                    palette = persisted.Palette;
                else
                    palette = bestColourRule(context)?.Palette;
            }

            return palette is { Length: > 0 } ? palette : null;
        }

        /// <summary>Resolves how a user's username should be rendered: a per-user override, then the best matching name rule, then normal.</summary>
        public UsernameNameRule ResolveName(IUser? user, APIUser? localUser)
        {
            if (user == null)
                return UsernameNameRule.Normal;

            var context = new Context(user, localUser);
            UsernameNameRule? rule;

            lock (lockObject)
            {
                if (sessionOverrides.TryGetValue(user.OnlineID, out var session) && session.NameRule != null)
                    rule = session.NameRule;
                else if (persistedOverrides.TryGetValue(user.OnlineID, out var persisted) && persisted.NameRule != null)
                    rule = persisted.NameRule;
                else
                    rule = bestNameRule(context)?.Rule;
            }

            return rule ?? UsernameNameRule.Normal;
        }

        private RuleHandle addColourRuleInternal(Func<IUsernameVisualsContext, bool> predicate, IReadOnlyList<Colour4> palette, int priority)
        {
            long id;

            lock (lockObject)
            {
                id = ++nextRuleId;
                colourRules.Add(new ColourRule(id, predicate, palette.ToArray(), priority));
            }

            return new RuleHandle(this, id, colour: true);
        }

        private RuleHandle addNameRuleInternal(Func<IUsernameVisualsContext, bool> predicate, UsernameNameRule rule, int priority)
        {
            long id;

            lock (lockObject)
            {
                id = ++nextRuleId;
                nameRules.Add(new NameRule(id, predicate, rule, priority));
            }

            return new RuleHandle(this, id, colour: false);
        }

        private void removeRule(long id, bool colour)
        {
            lock (lockObject)
            {
                if (colour)
                    colourRules.RemoveAll(r => r.Id == id);
                else
                    nameRules.RemoveAll(r => r.Id == id);
            }

            Changed?.Invoke();
        }

        private ColourRule? bestColourRule(IUsernameVisualsContext context)
        {
            ColourRule? best = null;

            foreach (var rule in colourRules)
            {
                if (!rule.Predicate(context))
                    continue;

                if (best == null || rule.Priority > best.Value.Priority || (rule.Priority == best.Value.Priority && rule.Id > best.Value.Id))
                    best = rule;
            }

            return best;
        }

        private NameRule? bestNameRule(IUsernameVisualsContext context)
        {
            NameRule? best = null;

            foreach (var rule in nameRules)
            {
                if (!rule.Predicate(context))
                    continue;

                if (best == null || rule.Priority > best.Value.Priority || (rule.Priority == best.Value.Priority && rule.Id > best.Value.Id))
                    best = rule;
            }

            return best;
        }

        /// <summary>Re-registers the settings-driven base conditionals with the current settings values.</summary>
        private void onSettingsChanged()
        {
            foreach (IDisposable handle in baseRuleHandles)
                handle.Dispose();

            baseRuleHandles.Clear();

            // Base rules only carry weight when a value is actually set; an empty rule would
            // otherwise shadow plugin-registered conditionals for the same users.
            var selfPaletteColours = SettingsSubsectionExtensions.ParsePalette(selfPalette.Value);
            if (selfPaletteColours.Length > 0)
                baseRuleHandles.Add(addColourRuleInternal(IsSelf, selfPaletteColours, selfPriority));

            var othersPaletteColours = SettingsSubsectionExtensions.ParsePalette(othersPalette.Value);
            if (othersPaletteColours.Length > 0)
                baseRuleHandles.Add(addColourRuleInternal(_ => true, othersPaletteColours, fallbackPriority));

            // The user's own display settings (hide, replace) always beat other plugins' rules.
            var ownNameRule = currentOwnNameRule();
            if (ownNameRule.Mode != UsernameNameMode.Normal)
                baseRuleHandles.Add(addNameRuleInternal(IsSelf, ownNameRule, userPreferencePriority));

            Changed?.Invoke();
        }

        private UsernameNameRule currentOwnNameRule()
        {
            if (hideEnabled.Value)
                return UsernameNameRule.Hide;

            return replaceEnabled.Value && !string.IsNullOrEmpty(replaceName.Value)
                ? UsernameNameRule.Replace(replaceName.Value)
                : UsernameNameRule.Normal;
        }

        private static bool IsSelf(IUsernameVisualsContext context)
            => context.User != null && context.LocalUser != null && context.User.OnlineID == context.LocalUser.OnlineID;

        private void applyPersistedOverrides()
        {
            lock (lockObject)
            {
                persistedOverrides.Clear();

                List<UsernameUserOverride>? parsed = null;

                try
                {
                    parsed = JsonSerializer.Deserialize<List<UsernameUserOverride>>(userOverrides.Value, jsonOptions);
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

                    var palette = SettingsSubsectionExtensions.ParsePalette(userOverride.Palette);
                    var nameRule = userOverride.Hide ? UsernameNameRule.Hide
                        : !string.IsNullOrEmpty(userOverride.Name) ? UsernameNameRule.Replace(userOverride.Name)
                        : (UsernameNameRule?)null;

                    if (palette.Length == 0 && nameRule == null)
                        continue;

                    persistedOverrides[userOverride.UserId] = new PerUserEntry { Palette = palette, NameRule = nameRule };
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

        private static PerUserEntry getOrCreate(Dictionary<int, PerUserEntry> map, int userId)
        {
            if (!map.TryGetValue(userId, out var entry))
                map[userId] = entry = new PerUserEntry();

            return entry;
        }

        private static void prune(Dictionary<int, PerUserEntry> map, int userId)
        {
            if (map.TryGetValue(userId, out var entry) && entry.Palette == null && entry.NameRule == null)
                map.Remove(userId);
        }

        private readonly record struct Context(IUser? User, APIUser? LocalUser) : IUsernameVisualsContext;

        private readonly record struct ColourRule(long Id, Func<IUsernameVisualsContext, bool> Predicate, Colour4[] Palette, int Priority);

        private readonly record struct NameRule(long Id, Func<IUsernameVisualsContext, bool> Predicate, UsernameNameRule Rule, int Priority);

        private sealed class PerUserEntry
        {
            public Colour4[]? Palette;
            public UsernameNameRule? NameRule;
        }

        /// <summary>Removes its rule on dispose; used by the base conditionals and by external registrations.</summary>
        private sealed class RuleHandle : IDisposable
        {
            private readonly UsernameVisualsApi owner;
            private readonly long id;
            private readonly bool colour;
            private bool disposed;

            public RuleHandle(UsernameVisualsApi owner, long id, bool colour)
            {
                this.owner = owner;
                this.id = id;
                this.colour = colour;
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                owner.removeRule(id, colour);
            }
        }
    }
}
