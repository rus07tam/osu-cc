using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;
using osucc.Client;
using osuTK;
using System;

namespace osuccDebug
{
    /// <summary>
    /// Debug panel for posting customised notifications, exercising
    /// <see cref="osucc.Plugin.IOsuCcPluginHost.Notify"/> so the plugin's own icon and name attach.
    /// </summary>
    public partial class NotificationTestPanel : FillFlowContainer
    {
        private readonly Bindable<string> message = new Bindable<string>("test notification");

        private readonly Bindable<ClientNotifications.NotificationKind> kind =
            new Bindable<ClientNotifications.NotificationKind>(ClientNotifications.NotificationKind.Info);

        private readonly Action<LocalisableString, ClientNotifications.NotificationKind> notify;

        public NotificationTestPanel(Action<LocalisableString, ClientNotifications.NotificationKind> notify)
        {
            this.notify = notify;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, 10);

            Children = new Drawable[]
            {
                new SettingsTextBox
                {
                    LabelText = osuccDebugStrings.NotificationMessageLabel,
                    Current = message,
                },
                new SettingsEnumDropdown<ClientNotifications.NotificationKind>
                {
                    LabelText = osuccDebugStrings.NotificationKindLabel,
                    Current = kind,
                },
                new SettingsButtonV2
                {
                    Text = osuccDebugStrings.PostNotificationButton,
                    Action = () => notify(message.Value, kind.Value),
                },
            };
        }
    }
}
