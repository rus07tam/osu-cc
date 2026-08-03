using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.Toolbar;
using osucc.Celebrations;
using osucc.Client;
using osucc.Core;
using osucc.Localisation;
using System;
using System.Reflection;

namespace osucc.Plugin
{
    /// <summary>Default <see cref="IOsuCcPluginHost"/> bound to a single <see cref="PluginEntry"/>.</summary>
    public class PluginHost : IOsuCcPluginHost
    {
        private readonly PluginEntry entry;

        private PluginSettings? settings;

        public PluginHost(PluginEntry entry)
        {
            this.entry = entry;
        }

        public string PluginId => entry.Id;

        public string PluginDirectory => entry.Directory;

        public void Log(string message) => TimingLog.Info($"[plugin:{entry.Name}] {message}");

        public void Notify(LocalisableString text, ClientNotifications.NotificationKind kind)
            => ClientNotifications.PostPlugin(text, kind, entry.Id, OsuCcLocalisation.Get($"{entry.Id}:name", entry.Name), resolveIcon(), resolveIconTexture());

        private IconUsage? resolveIcon() => (entry.Plugin as IOsuCcIconProvider)?.Icon;

        private Texture? resolveIconTexture()
        {
            if (!string.IsNullOrEmpty(entry.IconPath))
                return LoadTextureFromFile(entry.IconPath);

            if (!string.IsNullOrEmpty(entry.IconResource))
                return LoadTexture(entry.IconResource);

            return null;
        }

        public void Celebrate(Celebration celebration) => ClientCelebrations.Show(celebration);

        public void AddToolbarButton(Func<ToolbarButton> factory, ToolbarButtonPlacement placement = ToolbarButtonPlacement.Right, float? layoutPosition = null)
            => PluginManager.RegisterToolbarButton(factory, placement, layoutPosition);

        public void AddSettingsSubsection(Func<SettingsSubsection> factory) => PluginManager.RegisterSettingsSubsection(entry.Id, factory);

        public PluginSettings GetSettings()
            => settings ??= new PluginSettings(() => resolveStorage());

        public Storage? GetStorage(string subPath = "")
        {
            var storage = resolveStorage();
            return subPath.Length == 0 ? storage : storage?.GetStorageForDirectory(subPath);
        }

        /// <summary>This plugin's folder under the game's storage; <c>null</c> before the game attaches.</summary>
        private Storage? resolveStorage()
            => Reflection.GetStorage(ClientApi.Game)?.GetStorageForDirectory($"osu-cc/plugins/{entry.Id}");

        public HarmonyLib.Harmony CreateHarmony(string id) => HookDependencies.Create($"{entry.Id}.{id}");

        public Texture? LoadTexture(string resourceName)
        {
            try
            {
                var assembly = entry.Plugin?.GetType().Assembly;
                if (assembly == null || ClientApi.Game == null)
                    return null;

                using var stream = assembly.GetManifestResourceStream(resourceName);
                return stream == null ? null : TextureHelper.FromStream(stream);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PluginHost.LoadTexture ('{entry.Name}'): {ex}");
                return null;
            }
        }

        /// <summary>Loads a texture from an arbitrary file path (such as the plugin's own <c>icon.*</c>). Returns <c>null</c> on any failure.</summary>
        public Texture? LoadTextureFromFile(string path)
        {
            try
            {
                if (ClientApi.Game == null || !File.Exists(path))
                    return null;

                using var stream = File.OpenRead(path);
                return TextureHelper.FromStream(stream);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PluginHost.LoadTextureFromFile ('{entry.Name}'): {ex}");
                return null;
            }
        }

        public void RegisterBlockingOverlay(OverlayContainer overlay)
        {
            var scheduler = Reflection.GetScheduler(ClientApi.Game);

            if (scheduler == null)
            {
                TimingLog.Error($"PluginHost.RegisterBlockingOverlay ('{entry.Name}'): no scheduler available");
                return;
            }

            // The overlay content layer only exists after OsuGame.load; retry until it accepts the registration.
            scheduler.Add(() => tryRegisterOverlay(overlay, scheduler));
        }

        public void ExportApi(object api) => PluginManager.ExportPluginApi(entry.Id, api);

        public T? GetApi<T>(string pluginId) where T : class => PluginManager.GetPluginApi<T>(pluginId);

        private void tryRegisterOverlay(OverlayContainer overlay, Scheduler scheduler)
        {
            if (Reflection.RegisterBlockingOverlay(ClientApi.Game, overlay) != null)
            {
                TimingLog.Info($"PluginHost: blocking overlay registered for '{entry.Name}'");
                return;
            }

            scheduler.Add(() => tryRegisterOverlay(overlay, scheduler));
        }

        /// <summary>Re-reads persisted settings from disk once the game storage is available.</summary>
        internal void ReloadSettings() => settings?.Reload();
    }
}
