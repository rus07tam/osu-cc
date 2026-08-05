using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Overlays.Toolbar;
using System;

namespace OsuCcUpdater
{
    /// <summary>
    /// Toolbar button that runs an update check against the configured source and posts a toast
    /// with the outcome. Placed on the right edge, so the tooltip opens toward the screen centre.
    /// </summary>
    public partial class UpdaterToolbarButton : ToolbarButton
    {
        public UpdaterToolbarButton(Action check)
        {
            SetIcon(FontAwesome.Solid.CloudDownloadAlt);
            TooltipMain = OsuCcUpdaterStrings.TooltipMain;
            TooltipSub = OsuCcUpdaterStrings.TooltipSub;
            Action = check;
        }

        protected override Anchor TooltipAnchor => Anchor.TopRight;
    }
}
