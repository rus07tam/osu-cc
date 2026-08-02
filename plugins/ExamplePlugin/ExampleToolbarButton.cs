using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Overlays.Toolbar;
using osucc.Celebrations;
using osucc.Client;
using System;

namespace ExamplePlugin
{
    /// <summary>
    /// A toolbar button contributed by the plugin. The click action shows a celebration,
    /// honouring the plugin's "celebrate" setting.
    /// </summary>
    public partial class ExampleToolbarButton : ToolbarButton
    {
        private readonly Bindable<bool> celebrate;

        private readonly Action<LocalisableString, ClientNotifications.NotificationKind> notify;

        public ExampleToolbarButton(Bindable<bool> celebrate, Action<LocalisableString, ClientNotifications.NotificationKind> notify)
        {
            this.celebrate = celebrate;
            this.notify = notify;

            SetIcon(FontAwesome.Solid.Rocket);
            TooltipMain = ExamplePluginStrings.TooltipMain;
            TooltipSub = ExamplePluginStrings.TooltipSub;

            Action = () =>
            {
                if (celebrate.Value)
                {
                    ClientCelebrations.Show(new Celebration(new CelebrationOptions
                    {
                        TitleText = ExamplePluginStrings.CelebrationTitle,
                        SubtitleText = ExamplePluginStrings.CelebrationSubtitle,
                        AccentColour = Color4Extensions.FromHex("ff66cc"),
                    }));
                }
                else
                {
                    notify(ExamplePluginStrings.CelebrationsDisabled, ClientNotifications.NotificationKind.Info);
                }
            };
        }

        // Placed on the right edge, so open the tooltip toward the screen centre.
        protected override Anchor TooltipAnchor => Anchor.TopRight;
    }
}
