using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Overlays.Toolbar;
using osucc.Core;
using osucc.Plugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Injects plugin toolbar buttons once <c>Toolbar.load</c> has run, and again for buttons
    /// registered later (live plugin enable).
    /// </summary>
    public sealed class ToolbarLoadPatch : OsuCcPatch
    {
        private static Toolbar? toolbar;

        public ToolbarLoadPatch()
            : base("osu.Game.Overlays.Toolbar.Toolbar", "load", m => m.GetParameters().Length == 1, MethodType.Postfix)
        {
        }

        public static void Postfix(Toolbar __instance)
        {
            toolbar = __instance;

            foreach (var registration in PluginManager.ToolbarButtonRegistrations)
                AddPluginButton(registration);
        }

        /// <summary>
        /// Injects a plugin's button into the live toolbar. No-op until the toolbar has loaded; a
        /// button registered later (live enable) is injected immediately.
        /// </summary>
        internal static void AddPluginButton(ToolbarButtonRegistration registration)
        {
            if (toolbar == null)
                return;

            try
            {
                var leftButtons = findVisualChild(toolbar, "Left buttons");
                var rightButtons = findVisualChild(toolbar, "Right buttons");

                var leftFlow = leftButtons == null ? null : getChildren(leftButtons).OfType<FillFlowContainer>().FirstOrDefault();
                var rightFlow = rightButtons == null ? null : getChildren(rightButtons).OfType<FillFlowContainer>().FirstOrDefault();

                if (rightFlow == null)
                {
                    TimingLog.Error("ToolbarLoadPatch: FillFlowContainer in 'Right buttons' not found");
                    return;
                }

                if (leftFlow == null)
                    TimingLog.Info("ToolbarLoadPatch: FillFlowContainer in 'Left buttons' not found (no left-placed buttons?)");

                var button = registration.Factory();

                if (button == null)
                    return;

                var flow = registration.Placement == ToolbarButtonPlacement.Left ? leftFlow : rightFlow;

                if (flow == null)
                {
                    TimingLog.Error($"ToolbarLoadPatch: no flow for placement {registration.Placement} ({button.GetType().Name} skipped)");
                    return;
                }

                if (flow.Any(c => ReferenceEquals(c, button)))
                    return;

                flow.Add(button);

                if (registration.LayoutPosition is { } position)
                    flow.SetLayoutPosition(button, position);

                registration.RecordCreated(button);

                TimingLog.Info($"ToolbarLoadPatch: plugin button added ({button.GetType().Name}, {registration.Placement}, position={registration.LayoutPosition})");
            }
            catch (Exception ex)
            {
                TimingLog.Error($"ToolbarLoadPatch.AddPluginButton: {ex}");
            }
        }

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
            var childrenProp = drawable.GetType().GetProperty("Children", BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (childrenProp?.GetValue(drawable) is IEnumerable<Drawable> children)
            {
                foreach (var child in children)
                    yield return child;
            }

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
