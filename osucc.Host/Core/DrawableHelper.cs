using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using System.Collections;
using System.Reflection;

namespace osucc.Core
{
    /// <summary>
    /// Drawable-tree helpers shared by patches that swap a rendered child for a replacement
    /// (keeping its visual position) or search a composite's children by predicate.
    /// </summary>
    public static class DrawableHelper
    {
        /// <summary>
        /// Reads <c>Drawable.IsDisposed</c>, which is protected, reflectively. Returns
        /// <c>false</c> when the property cannot be read (including on disposed instances).
        /// </summary>
        public static bool IsDisposed(Drawable? drawable)
        {
            if (drawable == null)
                return false;

            try
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
                return (bool?)drawable.GetType().GetProperty("IsDisposed", flags)?.GetValue(drawable) ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Replaces a child within a flow, keeping its visual position (slightly earlier so it
        /// stays ahead of same-position siblings added after it).</summary>
        public static void SwapInFlow(FlowContainer<Drawable> flow, Drawable old, Drawable replacement)
        {
            float position = flow.GetLayoutPosition(old);
            flow.Remove(old, false);
            flow.Add(replacement);
            flow.SetLayoutPosition(replacement, position - 1);
        }

        /// <summary>
        /// Replaces a child within a grid by mutating the content cell in place (the grid
        /// relayouts itself when the cell is assigned).
        /// </summary>
        public static void SwapInGrid(GridContainer grid, Drawable old, Drawable replacement)
        {
            var content = grid.Content;

            for (int row = 0; row < content.Count; row++)
            {
                var cells = content[row];

                for (int column = 0; column < cells.Count; column++)
                {
                    if (ReferenceEquals(cells[column], old))
                    {
                        cells[column] = replacement;
                        return;
                    }
                }
            }
        }

        /// <summary>Replaces a drawable in whatever container hosts it.</summary>
        public static void SwapInParent(Drawable old, Drawable replacement)
        {
            if (old.Parent is not CompositeDrawable parent)
                return;

            switch (parent)
            {
                case GridContainer grid:
                    SwapInGrid(grid, old, replacement);
                    break;

                case FlowContainer<Drawable> flow:
                    SwapInFlow(flow, old, replacement);
                    break;

                default:
                    if (parent is Container<Drawable> container)
                    {
                        container.Remove(old, false);
                        container.Add(replacement);
                    }

                    break;
            }
        }

        /// <summary>Recursively searches a drawable tree for the first drawable matching the predicate.</summary>
        public static Drawable? FindInTree(Drawable root, Predicate<Drawable> match)
        {
            if (match(root))
                return root;

            foreach (Drawable child in getChildren(root))
            {
                var found = FindInTree(child, match);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>Walks the tree to find the grid whose content matrix holds the given drawable.</summary>
        public static GridContainer? FindGridContaining(Drawable root, Drawable target)
        {
            if (root is GridContainer grid && gridContains(grid, target))
                return grid;

            foreach (Drawable child in getChildren(root))
            {
                var found = FindGridContaining(child, target);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static readonly Lazy<PropertyInfo?> internalChildrenProperty = new(() =>
            typeof(CompositeDrawable).GetProperty("InternalChildren", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

        /// <summary>Enumerates the internal children of a composite, before/regardless of whether they are alive.</summary>
        private static IEnumerable<Drawable> getChildren(Drawable drawable)
        {
            if (drawable is not CompositeDrawable composite)
                yield break;

            if (internalChildrenProperty.Value?.GetValue(composite) is not IEnumerable children)
                yield break;

            foreach (object? child in children)
            {
                if (child is Drawable drawableChild)
                    yield return drawableChild;
            }
        }

        private static bool gridContains(GridContainer grid, Drawable target)
        {
            var content = grid.Content;

            for (int row = 0; row < content.Count; row++)
            {
                var cells = content[row];

                for (int column = 0; column < cells.Count; column++)
                {
                    if (ReferenceEquals(cells[column], target))
                        return true;
                }
            }

            return false;
        }
    }
}
