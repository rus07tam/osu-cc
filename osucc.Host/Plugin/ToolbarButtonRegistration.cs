using osu.Framework.Graphics.Containers;
using osu.Game.Overlays.Toolbar;

namespace osucc.Plugin
{
    /// <summary>A toolbar button registration made by a plugin, together with its placement options.</summary>
    public record ToolbarButtonRegistration(Func<ToolbarButton> Factory, ToolbarButtonPlacement Placement, float? LayoutPosition)
    {
        /// <summary>
        /// The buttons created from <see cref="Factory"/> that are currently live in the toolbar.
        /// Kept as weak references so a button removed by the toolbar itself does not linger.
        /// </summary>
        internal List<WeakReference<ToolbarButton>> CreatedButtons { get; } = new();

        /// <summary>Records a button created from this registration, skipping duplicates.</summary>
        internal void RecordCreated(ToolbarButton button)
        {
            if (CreatedButtons.Any(r => r.TryGetTarget(out var existing) && ReferenceEquals(existing, button)))
                return;

            CreatedButtons.Add(new WeakReference<ToolbarButton>(button));
        }

        /// <summary>Removes every live button created from this registration from the toolbar. Runs on the update thread.</summary>
        internal void RemoveCreated()
        {
            foreach (var reference in CreatedButtons)
            {
                if (reference.TryGetTarget(out var button) && button.Parent is FillFlowContainer flow)
                    flow.Remove(button, true);
            }

            CreatedButtons.Clear();
        }
    }
}
