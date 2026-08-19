using osu.Framework.Graphics.Containers;
using osu.Game.Overlays.Profile.Header.Components;
using osucc.Client;
using osucc.Core;
using osucc.Plugin;
using System;
using System.Collections.Concurrent;

namespace Oii
{
    /// <summary>
    /// Inserts an <see cref="OiiIndicator"/> into the profile header's main details flow right after
    /// the play time display, binding it to the same user data so it follows user/ruleset changes.
    /// </summary>
    [OsuCcPatch("osu.Game.Overlays.Profile.Header.Components.TotalPlayTime", "load")]
    internal static class TotalPlayTimeLoadPatch
    {
        private static IOsuCcPluginHost host = null!;

        private static readonly ConcurrentBag<WeakReference<OiiIndicator>> inserted = new();

        /// <summary>
        /// Removes every indicator this plugin inserted from its parent flow, so disabling the
        /// plugin leaves the header tree as it was. Runs from the plugin's <c>Dispose</c>, on the
        /// update thread.
        /// </summary>
        public static void RemoveIndicators()
        {
            foreach (var reference in inserted)
            {
                if (reference.TryGetTarget(out var indicator) && indicator.Parent is FillFlowContainer flow)
                    flow.Remove(indicator, true);
            }

            inserted.Clear();
        }

        private static void Postfix(TotalPlayTime __instance)
        {
            // Parent is only assigned after load() returns (CompositeDrawable.loadChild), so the
            // indicator is inserted on the first update frame, once the whole tree is loaded.
            var scheduler = Reflection.GetScheduler(ClientApi.Game);
            scheduler?.AddOnce(() => insertIndicator(__instance));
        }

        private static void insertIndicator(TotalPlayTime instance)
        {
            if (instance.Parent is not FillFlowContainer flow)
                return;

            var indicator = new OiiIndicator();
            indicator.User.BindTo(instance.User);
            inserted.Add(new WeakReference<OiiIndicator>(indicator));
            flow.Insert((int)flow.GetLayoutPosition(instance) + 1, indicator);

            host.Log(LogLevel.Info, "total playtime indicator inserted");
        }
    }
}
