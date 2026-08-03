using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Overlays.Toolbar;
using osucc.Client;
using System;

namespace MyPlugin;

/// <summary>A toolbar button contributed by the plugin; sends a notification when clicked.</summary>
public partial class MyToolbarButton : ToolbarButton
{
    private readonly Action<LocalisableString, NotificationKind> notify;

    public MyToolbarButton(Action<LocalisableString, NotificationKind> notify)
    {
        this.notify = notify;

        SetIcon(FontAwesome.Solid.Rocket);
        TooltipMain = MyPluginStrings.TooltipMain;
        TooltipSub = MyPluginStrings.TooltipSub;

        Action = () => notify(MyPluginStrings.HelloNotification, NotificationKind.Success);
    }

    // Placed on the right edge, so open the tooltip toward the screen centre.
    protected override Anchor TooltipAnchor => Anchor.TopRight;
}
