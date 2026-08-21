using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osucc.Core;
using osucc.Localisation;
using osucc.Plugin;
using osuTK;
using osuTK.Graphics;

namespace osucc.UI.Plugins
{
    /// <summary>
    /// Compact badge/pill displaying diagnostic count (error, warning, notice) with an icon and tooltip.
    /// </summary>
    public partial class DiagnosticPill : CompositeDrawable, IHasTooltip
    {
        public LocalisableString TooltipText { get; }

        private readonly Box background;
        private readonly Color4 accentColour;

        public DiagnosticPill(PluginDiagnosticLevel level, int count)
        {
            AutoSizeAxes = Axes.Both;
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;
            Masking = true;
            CornerRadius = 4;

            (IconUsage icon, accentColour, TooltipText) = level switch
            {
                PluginDiagnosticLevel.Error => (FontAwesome.Solid.TimesCircle, OsuCcColours.Error, PluginsOverlayStrings.DiagnosticsErrorCount(count)),
                PluginDiagnosticLevel.Warning => (FontAwesome.Solid.ExclamationTriangle, OsuCcColours.Warning, PluginsOverlayStrings.DiagnosticsWarningCount(count)),
                _ => (FontAwesome.Solid.InfoCircle, OsuCcColours.Info, PluginsOverlayStrings.DiagnosticsNoticeCount(count)),
            };

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = accentColour.Opacity(0.25f),
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(3, 0),
                    Padding = new MarginPadding { Horizontal = 4, Vertical = 2 },
                    Children = new Drawable[]
                    {
                        new SpriteIcon
                        {
                            Icon = icon,
                            Size = new Vector2(9),
                            Colour = accentColour,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                        new OsuSpriteText
                        {
                            Text = count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            Font = OsuFont.Torus.With(size: 9, weight: FontWeight.Bold),
                            Colour = Color4.White,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                    },
                },
            };
        }
    }
}
