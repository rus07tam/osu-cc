using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Overlays.Toolbar;
using System;

namespace osuccDebug
{
    /// <summary>
    /// Toolbar button that toggles the osu!cc debug overlay. Placed on the right edge, so the
    /// tooltip opens toward the screen centre (like the game's own right-hand buttons).
    /// </summary>
    public partial class osuccDebugButton : ToolbarButton
    {
        public osuccDebugButton(Action toggle)
        {
            SetIcon(FontAwesome.Solid.Bug);
            TooltipMain = osuccDebugStrings.TooltipMain;
            TooltipSub = osuccDebugStrings.TooltipSub;
            Action = toggle;
        }

        protected override Anchor TooltipAnchor => Anchor.TopRight;
    }
}
