using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays.Dialog;
using osucc.Localisation;
using osuTK.Graphics;
using System;

namespace osucc.Client
{
    /// <summary>
    /// First-run disclaimer shown once (until <see cref="SpecialsSetting.FirstRunSetupComplete"/>
    /// is set). Rendered through the game's own <see cref="PopupDialog"/> so it matches the stock
    /// dialog styling, with a multi-coloured <see cref="OsuTextFlowContainer"/> body.
    /// </summary>
    public partial class OsuCcDisclaimerDialog : PopupDialog
    {
        private static readonly Color4 reassuranceColour = Color4Extensions.FromHex("88b300"); // OsuColour.Green
        private static readonly Color4 cautionColour = Color4Extensions.FromHex("ffcc22"); // OsuColour.Yellow
        private static readonly Color4 dangerColour = Color4Extensions.FromHex("ed1121"); // OsuColour.Red
        private static readonly Color4 bodyColour = Color4.White;

        private readonly OsuTextFlowContainer bodyText;

        public OsuCcDisclaimerDialog(Action confirmed)
        {
            HeaderText = FirstRunStrings.WelcomeTitle;
            Icon = FontAwesome.Solid.InfoCircle;

            bodyText = new OsuTextFlowContainer(t => t.Font = t.Font.With(size: 18))
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                TextAnchor = Anchor.TopCentre,
            };

            MainContent.Child = bodyText;

            Buttons = new PopupDialogButton[]
            {
                new PopupDialogOkButton
                {
                    Text = FirstRunStrings.UnderstandButton,
                    Action = confirmed,
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            bodyText.AddText(FirstRunStrings.BodyStart, t => t.Colour = bodyColour);
            bodyText.AddText(FirstRunStrings.BodyNotCheat, t => t.Colour = reassuranceColour);
            bodyText.AddText(FirstRunStrings.BodyNotCheatRest, t => t.Colour = bodyColour);
            bodyText.NewLine();
            bodyText.NewLine();
            bodyText.AddText(FirstRunStrings.BodyUnstableStart, t => t.Colour = bodyColour);
            bodyText.AddText(FirstRunStrings.BodyUnstable, t => t.Colour = cautionColour);
            bodyText.AddText(FirstRunStrings.BodyUnstableRest, t => t.Colour = bodyColour);
            bodyText.NewLine();
            bodyText.NewLine();
            bodyText.AddText(FirstRunStrings.BodyRiskStart, t => t.Colour = bodyColour);
            bodyText.AddText(FirstRunStrings.BodyNoGuarantee, t => t.Colour = dangerColour);
            bodyText.AddText(FirstRunStrings.BodyRiskRest, t => t.Colour = bodyColour);
            bodyText.AddText(FirstRunStrings.BodyViolatesRules, t => t.Colour = dangerColour);
            bodyText.AddText(FirstRunStrings.BodyRiskEnd, t => t.Colour = bodyColour);
        }
    }
}
