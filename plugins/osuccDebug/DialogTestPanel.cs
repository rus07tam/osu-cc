using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Settings;
using osucc.Client;
using osucc.Plugin;
using osuTK;

namespace osuccDebug
{
    /// <summary>
    /// Debug panel for exercising the plugin dialog API: a destructive confirmation
    /// (<see cref="osucc.Plugin.IOsuCcPluginHost.Confirm"/>), a restart-style confirm
    /// (<see cref="osucc.Plugin.IOsuCcPluginHost.Restart"/>) and a plugin-defined
    /// <see cref="PopupDialog"/> pushed through the generic
    /// <see cref="osucc.Plugin.IOsuCcPluginHost.Push"/>.
    /// </summary>
    public partial class DialogTestPanel : FillFlowContainer
    {
        private readonly IOsuCcPluginHost host;

        public DialogTestPanel(IOsuCcPluginHost host)
        {
            this.host = host;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, 10);

            Children = new Drawable[]
            {
                new SettingsButtonV2
                {
                    Text = osuccDebugStrings.DialogConfirmButton,
                    Action = () => host.Confirm(
                        osuccDebugStrings.DialogConfirmTitle,
                        osuccDebugStrings.DialogConfirmBody,
                        () => host.Notify(osuccDebugStrings.DialogConfirmed, NotificationKind.Success)),
                },
                new SettingsButtonV2
                {
                    Text = osuccDebugStrings.DialogRestartButton,
                    Action = () => host.Restart(
                        osuccDebugStrings.DialogRestartTitle,
                        osuccDebugStrings.DialogRestartBody,
                        osuccDebugStrings.DialogOk,
                        () => host.Notify(osuccDebugStrings.DialogRestarted, NotificationKind.Info)),
                },
                new SettingsButtonV2
                {
                    Text = osuccDebugStrings.DialogPushButton,
                    Action = () => host.Push(new DebugPopupDialog()),
                },
            };
        }
    }

    /// <summary>A minimal <see cref="PopupDialog"/> defined by a plugin and pushed via <see cref="IOsuCcPluginHost.Push"/>.</summary>
    public partial class DebugPopupDialog : PopupDialog
    {
        public DebugPopupDialog()
        {
            HeaderText = osuccDebugStrings.DialogPushTitle;
            BodyText = osuccDebugStrings.DialogPushBody;
            Icon = FontAwesome.Solid.InfoCircle;

            Buttons = new PopupDialogButton[]
            {
                new PopupDialogOkButton
                {
                    Text = osuccDebugStrings.DialogOk,
                },
            };
        }
    }
}
