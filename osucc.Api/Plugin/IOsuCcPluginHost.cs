using HarmonyLib;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osu.Game;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.Toolbar;
using osucc.Celebrations;
using osucc.Client;
using osucc.Core;
using System;
using System.Reflection;

namespace osucc.Plugin
{
    /// <summary>
    /// Scoped surface given to each plugin. Every call is bound to the calling plugin
    /// (id, storage folder, log prefix), so plugins stay isolated. Registrations and patches
    /// are tracked by the host so they can be revoked on live disable.
    /// </summary>
    public interface IOsuCcPluginHost
    {
        /// <summary>Stable id of the loaded plugin (from <see cref="OsuCcPluginAttribute.Id"/>).</summary>
        string PluginId { get; }

        /// <summary>Directory the plugin's DLL and assets live in — its own subfolder under the osu-cc "plugins" directory.</summary>
        string PluginDirectory { get; }

        /// <summary>The live game instance, or <c>null</c> before the game attaches.</summary>
        OsuGameBase? Game { get; }

        /// <summary>The game's update-thread scheduler, or <c>null</c> before the game attaches. Run drawable/UI work through this.</summary>
        Scheduler? Scheduler { get; }

        /// <summary>Resolves a service from the game's dependency container, or <c>null</c> when it is not available.</summary>
        T? GetDependency<T>() where T : class;

        /// <summary>Client lifecycle events a plugin can subscribe to.</summary>
        IOsuCcPluginEvents Events { get; }

        /// <summary>Logs a line with the plugin's name prefix.</summary>
        void Log(string message);

        /// <summary>Posts a toast into the game's notification overlay.</summary>
        void Notify(LocalisableString text, NotificationKind kind);

        /// <summary>Shows a full-screen celebration over the game's top-most overlay content.</summary>
        void Celebrate(Celebration celebration);

        /// <summary>
        /// Shows a destructive-action confirmation (hold-to-confirm button). Returns <c>false</c> when the
        /// game or its dialog overlay is not available yet, in which case the dialog was not shown.
        /// </summary>
        bool Confirm(LocalisableString title, LocalisableString body, Action confirmed);

        /// <summary>
        /// Shows a non-destructive confirm for actions that need a restart. Returns <c>false</c> when the
        /// game or its dialog overlay is not available yet, in which case the dialog was not shown.
        /// </summary>
        bool Restart(LocalisableString title, LocalisableString body, LocalisableString confirmText, Action confirmed);

        /// <summary>
        /// Queues an arbitrary <see cref="osu.Game.Overlays.Dialog.PopupDialog"/> onto the game's dialog
        /// overlay. Safe to call from any thread; the push happens on the update thread. Returns <c>false</c>
        /// when the game, overlay or scheduler is not available yet, in which case the dialog was not shown.
        /// </summary>
        bool Push(osu.Game.Overlays.Dialog.PopupDialog dialog);

        /// <summary>
        /// Registers a toolbar button with explicit placement. Negative <paramref name="layoutPosition"/>
        /// places the button earlier (first = <c>-1f</c>), positive later; <c>null</c> appends at the end.
        /// For right-edge buttons, override <c>TooltipAnchor =&gt; Anchor.TopRight</c> so the tooltip opens
        /// toward the screen centre instead of off the right edge. Disposing the returned handle
        /// revokes the registration.
        /// </summary>
        IDisposable AddToolbarButton(Func<ToolbarButton> button, ToolbarButtonPlacement placement = ToolbarButtonPlacement.Right, float? layoutPosition = null);

        /// <summary>Registers a settings subsection shown inside the "Specials" settings section, after the built-in subsections. Disposing the returned handle revokes it.</summary>
        IDisposable AddSettingsSubsection(Func<SettingsSubsection> factory);

        /// <summary>
        /// The plugin's ini-backed key-value settings store. Defaults can be registered during
        /// <see cref="IOsuCcPlugin.Load"/>; persisted values are loaded on <see cref="IOsuCcPlugin.AttachToGame"/>.
        /// </summary>
        PluginSettings GetSettings();

        /// <summary>
        /// The VFS storage for this plugin's configuration and resources.
        /// </summary>
        osucc.Data.IOsuCcStorage Data { get; }

        /// <summary>A storage folder under the game's storage, dedicated to this plugin. <c>null</c> before the game attaches.</summary>
        Storage? GetStorage(string subPath = "");

        /// <summary>
        /// Applies a Harmony patch scoped to this plugin and tracks it so the host can revert it
        /// on live disable. The target is patched on a per-plugin Harmony instance, so disabling
        /// this plugin never touches patches of the built-in client or other plugins. Returns
        /// <c>null</c> when the target could not be patched; disposing the returned handle
        /// unpatchs it. Prefer the convenience helpers in <see cref="PatchHelper"/> for the
        /// common name-based shape.
        /// </summary>
        IDisposable? AddPatch(MethodBase target, Type patchType, string patchMethodName, osucc.Core.MethodType type);

        /// <summary>
        /// Loads a texture from this plugin's embedded assembly resources. <c>null</c> if the game is
        /// not attached yet or the resource is missing.
        /// </summary>
        Texture? LoadTexture(string resourceName);

        /// <summary>
        /// Registers a full-screen blocking overlay with the game's overlay manager. The overlay must not
        /// have a parent. Registration is retried on the update thread until the game's overlay content
        /// layer exists, so it is safe to call before <c>OsuGame.load</c> has finished. Disposing the
        /// returned handle stops retries and unregisters the overlay if it was registered.
        /// </summary>
        IDisposable RegisterBlockingOverlay(OverlayContainer overlay);

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
