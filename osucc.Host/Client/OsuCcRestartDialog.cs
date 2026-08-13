using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Overlays.Dialog;
using osucc.Localisation;

namespace osucc.Client
{
    /// <summary>
    /// Non-destructive confirm for actions that need the game to restart (e.g. changing the
    /// <see cref="Core.OsuCcThemeManager.Active"/>). Uses the stock <see cref="PopupDialogOkButton"/>/cancel
    /// pair and a neutral icon, unlike <see cref="OsuCcConfirmDialog"/> which is deliberately
    /// destructive (delete), so it carries its own "Delete"/trash styling.
    /// </summary>
    public partial class OsuCcRestartDialog : PopupDialog
    {
        public OsuCcRestartDialog(LocalisableString title, LocalisableString body, LocalisableString confirmText, Action confirmed)
        {
            HeaderText = title;
            BodyText = body;
            Icon = FontAwesome.Solid.ExclamationTriangle;

            Buttons = new PopupDialogButton[]
            {
                new PopupDialogOkButton
                {
                    Text = confirmText,
                    Action = confirmed,
                },
                new PopupDialogCancelButton
                {
                    Text = SpecialsSettingsStrings.ThemeCancelButton,
                },
            };
        }
    }
}
