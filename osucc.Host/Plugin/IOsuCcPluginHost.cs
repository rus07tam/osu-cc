using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.Toolbar;
using osucc.Celebrations;
using osucc.Client;
using System;

namespace osucc.Plugin
{
    /// <summary>
    /// Scoped surface given to each plugin. Every call is bound to the calling plugin
    /// (id, storage folder, harmony id prefix, log prefix), so plugins stay isolated.
    /// </summary>
    public interface IOsuCcPluginHost
    {
        /// <summary>Stable id of the loaded plugin (from <see cref="OsuCcPluginAttribute.Id"/>).</summary>
        string PluginId { get; }

        /// <summary>Directory the plugin's DLL and assets live in — its own subfolder under the osu-cc "plugins" directory.</summary>
        string PluginDirectory { get; }

        /// <summary>Logs a line with the plugin's name prefix.</summary>
        void Log(string message);

        /// <summary>Posts a toast into the game's notification overlay.</summary>
        void Notify(LocalisableString text, ClientNotifications.NotificationKind kind);

        /// <summary>Shows a full-screen celebration over the game's top-most overlay content.</summary>
        void Celebrate(Celebration celebration);

        /// <summary>
        /// Registers a toolbar button with explicit placement. Negative <paramref name="layoutPosition"/>
        /// places the button earlier (first = <c>-1f</c>), positive later; <c>null</c> appends at the end.
        /// For right-edge buttons, override <c>TooltipAnchor =&gt; Anchor.TopRight</c> so the tooltip opens
        /// toward the screen centre instead of off the right edge.
        /// </summary>
        void AddToolbarButton(Func<ToolbarButton> factory, ToolbarButtonPlacement placement = ToolbarButtonPlacement.Right, float? layoutPosition = null);

        /// <summary>Registers a settings subsection shown inside the "Specials" settings section, after the built-in subsections.</summary>
        void AddSettingsSubsection(Func<SettingsSubsection> factory);

        /// <summary>
        /// The plugin's ini-backed key-value settings store. Defaults can be registered during
        /// <see cref="IOsuCcPlugin.Load"/>; persisted values are loaded on <see cref="IOsuCcPlugin.AttachToGame"/>.
        /// </summary>
        PluginSettings GetSettings();

        /// <summary>A storage folder under the game's storage, dedicated to this plugin. <c>null</c> before the game attaches.</summary>
        Storage? GetStorage(string subPath = "");

        /// <summary>Creates a Harmony instance scoped to this plugin for patching osu methods by name.</summary>
        HarmonyLib.Harmony CreateHarmony(string id);

        /// <summary>
        /// Loads a texture from this plugin's embedded assembly resources. <c>null</c> if the game is
        /// not attached yet or the resource is missing.
        /// </summary>
        Texture? LoadTexture(string resourceName);

        /// <summary>
        /// Registers a full-screen blocking overlay with the game's overlay manager. The overlay must not
        /// have a parent. Registration is retried on the update thread until the game's overlay content
        /// layer exists, so it is safe to call before <c>OsuGame.load</c> has finished.
        /// </summary>
        void RegisterBlockingOverlay(OverlayContainer overlay);

        /// <summary>
        /// Exports an object as this plugin's public API. Other plugins fetch it by this plugin's id
        /// via <see cref="GetApi{T}"/>. Re-exporting a new instance of the same concrete type replaces
        /// the previous export. Export during <see cref="IOsuCcPlugin.Load"/> so it is ready for other
        /// plugins' <see cref="IOsuCcPlugin.Load"/> / <see cref="IOsuCcPlugin.AttachToGame"/>.
        /// </summary>
        void ExportApi(object api);

        /// <summary>
        /// Fetches an API object exported by the plugin with the given id (see <see cref="ExportApi"/>).
        /// Returns <c>null</c> when the plugin is not loaded or exported nothing assignable to
        /// <typeparamref name="T"/>. Safe to call from <see cref="IOsuCcPlugin.Load"/> when the exporting
        /// plugin's priority guarantees it loaded first; always safe from <see cref="IOsuCcPlugin.AttachToGame"/>.
        /// </summary>
        T? GetApi<T>(string pluginId) where T : class;
    }
}
