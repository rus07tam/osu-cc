using osu.Framework.Bindables;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Users;
using osucc.Plugin;
using osuTK.Graphics;

namespace UsernameVisuals
{
    /// <summary>
    /// Central configuration and palette resolution for the username visuals plugin.
    /// Reads the plugin's ini-backed settings and decides, for a given user, which palette
    /// (if any) should be applied and how the own username should be rendered (replace/hide).
    /// Palette lists are immutable snapshots so readers on any thread see a consistent value.
    /// </summary>
    public static class UsernameVisualsResolver
    {
        private const string masterEnabledKey = "gradient_enabled";
        private const string selfPaletteKey = "self_palette";
        private const string othersPaletteKey = "others_palette";
        private const string replaceEnabledKey = "own_replace_enabled";
        private const string replaceNameKey = "own_replace_name";
        private const string hideEnabledKey = "own_hide_enabled";

        private static readonly object lockObject = new();
        private static readonly string emptyPalette = string.Empty;

        private static Bindable<bool> masterEnabled = new(false);
        private static Bindable<string> selfPalette = new(emptyPalette);
        private static Bindable<string> othersPalette = new(emptyPalette);
        private static Bindable<bool> replaceEnabled = new(false);
        private static Bindable<string> replaceName = new(emptyPalette);
        private static Bindable<bool> hideEnabled = new(false);

        private static Color4[] selfColours = Array.Empty<Color4>();
        private static Color4[] othersColours = Array.Empty<Color4>();

        /// <summary>Whether gradient rendering is enabled at all.</summary>
        public static bool Enabled => masterEnabled.Value;

        /// <summary>Whether the own username is replaced with <see cref="ReplaceName"/>.</summary>
        public static bool ReplaceEnabled => replaceEnabled.Value;

        /// <summary>The text shown instead of the own username when <see cref="ReplaceEnabled"/>.</summary>
        public static string ReplaceName => replaceName.Value;

        /// <summary>Whether the own username is hidden behind a solid block.</summary>
        public static bool HideEnabled => hideEnabled.Value;

        /// <summary>Fired whenever any relevant setting changes; texts re-resolve their palette.</summary>
        public static event Action? Changed;

        /// <summary>
        /// Wires the resolver to the plugin's persisted settings. Called once during
        /// <see cref="IOsuCcPlugin.Load"/>.
        /// </summary>
        public static void Attach(PluginSettings settings)
        {
            masterEnabled = settings.Bind(masterEnabledKey, false);
            selfPalette = settings.Bind(selfPaletteKey, emptyPalette);
            othersPalette = settings.Bind(othersPaletteKey, emptyPalette);
            replaceEnabled = settings.Bind(replaceEnabledKey, false);
            replaceName = settings.Bind(replaceNameKey, emptyPalette);
            hideEnabled = settings.Bind(hideEnabledKey, false);

            masterEnabled.ValueChanged += _ => onConfigChanged();
            selfPalette.ValueChanged += _ => onConfigChanged();
            othersPalette.ValueChanged += _ => onConfigChanged();
            replaceEnabled.ValueChanged += _ => onConfigChanged();
            replaceName.ValueChanged += _ => onConfigChanged();
            hideEnabled.ValueChanged += _ => onConfigChanged();

            onConfigChanged();
        }

        /// <summary>
        /// Resolves the palette for a user, or <c>null</c> if gradient rendering should not
        /// apply (disabled, no user, or an empty palette).
        /// </summary>
        public static IReadOnlyList<Color4>? Resolve(IUser? user, APIUser? localUser)
        {
            if (!Enabled || user == null)
                return null;

            Color4[] palette = localUser != null && user.OnlineID == localUser.OnlineID ? selfColours : othersColours;
            return palette.Length == 0 ? null : palette;
        }

        /// <summary>Parses a comma-separated hex colour string into an immutable palette.</summary>
        public static Color4[] Parse(string? value) => SettingsSubsectionExtensions.ParsePalette(value);

        /// <summary>How the own username should be rendered for the current configuration.</summary>
        public enum OwnNameMode
        {
            /// <summary>Show the real username.</summary>
            Normal,

            /// <summary>Show <see cref="ReplaceName"/> instead of the real username.</summary>
            Replace,

            /// <summary>Hide the username behind a solid block.</summary>
            Hide,
        }

        /// <summary>
        /// Resolves how a user's username should be rendered. Own-display effects (replace/hide)
        /// apply to the local user only; other users always render normally.
        /// </summary>
        public static OwnNameMode OwnModeFor(IUser? user, APIUser? localUser)
        {
            if (user == null || localUser == null || user.OnlineID != localUser.OnlineID)
                return OwnNameMode.Normal;

            if (HideEnabled)
                return OwnNameMode.Hide;

            return ReplaceEnabled && !string.IsNullOrEmpty(ReplaceName) ? OwnNameMode.Replace : OwnNameMode.Normal;
        }

        private static void onConfigChanged()
        {
            lock (lockObject)
            {
                selfColours = Parse(selfPalette.Value);
                othersColours = Parse(othersPalette.Value);
            }

            Changed?.Invoke();
        }
    }
}
