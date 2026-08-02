using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays.Dialog;
using osucc.Localisation;
using osuTK.Graphics;
using System;

namespace osucc.Client
{
    /// <summary>
    /// Reusable destructive-action confirmation, rendered through the game's own
    /// <see cref="PopupDialog"/> so it matches the stock dialog styling. The confirming button
    /// is the game's hold-to-confirm <see cref="PopupDialogDangerousButton"/>.
    /// </summary>
    public partial class OsuCcConfirmDialog : PopupDialog
    {
        private readonly OsuTextFlowContainer bodyText;
        private readonly LocalisableString body;

        public OsuCcConfirmDialog(LocalisableString title, LocalisableString body, Action confirmed)
        {
            HeaderText = title;
            Icon = FontAwesome.Solid.Trash;

            this.body = body;

            bodyText = new OsuTextFlowContainer(t => t.Font = t.Font.With(size: 18))
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                TextAnchor = Anchor.TopCentre,
            };

            MainContent.Child = bodyText;

            Buttons = new PopupDialogButton[]
            {
                new PopupDialogDangerousButton
                {
                    Text = OsuCcStrings.Delete,
                    Action = confirmed,
                },
                new PopupDialogCancelButton
                {
                    Text = OsuCcStrings.Cancel,
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            bodyText.AddText(body, t => t.Colour = Color4.White);
        }
    }
}
