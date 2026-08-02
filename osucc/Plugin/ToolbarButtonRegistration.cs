using osu.Game.Overlays.Toolbar;

namespace osucc.Plugin
{
    /// <summary>A toolbar button registration made by a plugin, together with its placement options.</summary>
    public record ToolbarButtonRegistration(Func<ToolbarButton> Factory, ToolbarButtonPlacement Placement, float? LayoutPosition);
}
