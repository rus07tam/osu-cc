using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osucc.Core;
using osuTK.Graphics;

namespace osucc.Client
{
    /// <summary>
    /// Public notification API. Posts into the game's <see cref="INotificationOverlay"/>
    /// (resolved reflectively from the live game instance).
    /// </summary>
    public static class ClientNotifications
    {
        public static void Success(LocalisableString text) => Post(text, NotificationKind.Success);

        public static void Error(LocalisableString text) => Post(text, NotificationKind.Error);

        public static void Warning(LocalisableString text) => Post(text, NotificationKind.Warning);

        public static void Info(LocalisableString text) => Post(text, NotificationKind.Info);

        public static void Post(LocalisableString text, NotificationKind kind) => Post(text, kind, null, null, null);

        /// <summary>
        /// Posts a notification carrying the calling plugin's icon (FontAwesome or texture),
        /// with the plugin name as a clickable title line that opens the plugin manager and
        /// focuses the plugin. Falls back to the generic kind icon when none is available.
        /// Called via <see cref="Plugin.PluginHost.Notify"/>.
        /// </summary>
        internal static void PostPlugin(LocalisableString text, NotificationKind kind, string pluginId, LocalisableString title, IconUsage? icon, Texture? iconTexture)
            => Post(text, kind, title, icon, iconTexture, pluginId);

        private static void Post(LocalisableString text, NotificationKind kind, LocalisableString? title, IconUsage? icon, Texture? iconTexture, string? pluginId = null)
        {
            var game = ClientApi.Game;

            if (game == null)
            {
                TimingLog.Error($"Notifications: no game instance available; \"{text}\" dropped");
                return;
            }

            var overlay = Reflection.GetNotificationOverlay(game);

            if (overlay == null)
            {
                TimingLog.Error($"Notifications: overlay unavailable; \"{text}\" dropped");
                return;
            }

            Color4 colour = kind switch
            {
                NotificationKind.Success => OsuCcColours.Success,
                NotificationKind.Error => OsuCcColours.Error,
                NotificationKind.Warning => OsuCcColours.Warning,
                _ => OsuCcColours.Info,
            };

            Notification notification;

            if (icon == null && iconTexture == null)
            {
                notification = kind == NotificationKind.Error
                    ? new SimpleErrorNotification
                    {
                        Text = text,
                        Icon = FontAwesome.Solid.Bomb,
                        IconColour = OsuCcColours.Error,
                    }
                    : new SimpleNotification
                    {
                        Text = text,
                        Icon = kind switch
                        {
                            NotificationKind.Success => FontAwesome.Solid.CheckCircle,
                            NotificationKind.Warning => FontAwesome.Solid.ExclamationTriangle,
                            _ => FontAwesome.Solid.InfoCircle,
                        },
                        IconColour = colour,
                    };
            }
            else
            {
                notification = new PluginNotification(icon, iconTexture, colour, pluginId ?? string.Empty)
                {
                    Text = text,
                    Title = title ?? string.Empty,
                };
            }

            overlay.Post(notification);
            TimingLog.Info($"Notification posted{(kind == NotificationKind.Error ? " (error)" : "")}: {text}");
        }
    }
}
