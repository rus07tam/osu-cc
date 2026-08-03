using HarmonyLib;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Overlays.Toolbar;
using osucc.Core;
using osucc.Plugin;
using System.Collections;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Injects plugin toolbar buttons once <c>Toolbar.load</c> has run. Both button groups are
    /// located by their stable <c>Name = "Left buttons"</c> / <c>Name = "Right buttons"</c>
    /// markers, and each registered plugin button is added to the matching
    /// <see cref="FillFlowContainer"/> with its requested layout position.
    /// </summary>
    public static class ToolbarLoadPatch
    {
        public static bool Install()
        {
            var load = Reflection.GetMethod("osu.Game.Overlays.Toolbar.Toolbar", "load", m => m.GetParameters().Length == 1);
            if (load == null)
            {
                TimingLog.Error("ToolbarLoadPatch: Toolbar.load(..) method not found");
                return false;
            }

            HookDependencies.Main.Patch(load, postfix: Reflection.HarmonyMethod(typeof(ToolbarLoadPatch), nameof(Postfix)));
            TimingLog.Info("Toolbar.load patched (postfix)");
            return true;
        }

        private static void Postfix(Toolbar __instance)
        {
            try
            {
                var leftButtons = findVisualChild(__instance, "Left buttons");
                var rightButtons = findVisualChild(__instance, "Right buttons");

                var leftFlow = leftButtons == null ? null : getChildren(leftButtons).OfType<FillFlowContainer>().FirstOrDefault();
                var rightFlow = rightButtons == null ? null : getChildren(rightButtons).OfType<FillFlowContainer>().FirstOrDefault();

                if (rightFlow == null)
                {
                    TimingLog.Error("ToolbarLoadPatch: FillFlowContainer in 'Right buttons' not found");
                    return;
                }

                if (leftFlow == null)
                    TimingLog.Info("ToolbarLoadPatch: FillFlowContainer in 'Left buttons' not found (no left-placed buttons?)");

                foreach (var registration in PluginManager.ToolbarButtonRegistrations)
                {
                    try
                    {
                        var button = registration.Factory();

                        if (button == null)
                            continue;

                        var flow = registration.Placement == ToolbarButtonPlacement.Left ? leftFlow : rightFlow;

                        if (flow == null)
                        {
                            TimingLog.Error($"ToolbarLoadPatch: no flow for placement {registration.Placement} ({button.GetType().Name} skipped)");
                            continue;
                        }

                        if (flow.Any(c => ReferenceEquals(c, button)))
                            continue;

                        flow.Add(button);

                        if (registration.LayoutPosition is { } position)
                            flow.SetLayoutPosition(button, position);

                        TimingLog.Info($"ToolbarLoadPatch: plugin button added ({button.GetType().Name}, {registration.Placement}, position={registration.LayoutPosition})");
                    }
                    catch (Exception ex)
                    {
                        TimingLog.Error($"ToolbarLoadPatch.AddPluginButtons: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                TimingLog.Error($"ToolbarLoadPatch.Postfix: {ex}");
            }
        }

        /// <summary>
        /// Walks a drawable's visual children (and a <c>GridContainer</c>'s grid content,
        /// which is stored separately from its children) looking for a drawable with the
        /// given <c>Name</c>.
        /// </summary>
        private static Drawable? findVisualChild(Drawable root, string name)
        {
            foreach (var child in getChildren(root))
            {
                if (Reflection.GetName(child) == name)
                    return child;

                var nested = findVisualChild(child, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static IEnumerable<Drawable> getChildren(Drawable drawable)
        {
            // Regular container children (Container<T> exposes Children as IReadOnlyList<Drawable>).
            var childrenProp = drawable.GetType().GetProperty("Children", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (childrenProp?.GetValue(drawable) is IEnumerable<Drawable> children)
            {
                foreach (var child in children)
                    yield return child;
            }

            // GridContainer has no Children of its own; its content lives in the public
            // `Content` property, whose runtime type is GridContainerContent — an enumerable
            // of rows (ObservableArray<Drawable>), each row being an enumerable of cells.
            var contentProp = drawable.GetType().GetProperty("Content", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (contentProp?.GetValue(drawable) is IEnumerable content)
            {
                foreach (var row in content)
                {
                    if (row is Drawable cellDrawable)
                    {
                        yield return cellDrawable;
                    }
                    else if (row is IEnumerable rowContent)
                    {
                        foreach (var cell in rowContent)
                        {
                            if (cell is Drawable inner)
                                yield return inner;
                        }
                    }
                }
            }
        }
    }
}
