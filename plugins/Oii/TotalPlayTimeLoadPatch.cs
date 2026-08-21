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
    public sealed class TotalPlayTimeLoadPatch : PluginPatch<OiiPlugin>
    {
        private static readonly ConcurrentBag<WeakReference<OiiIndicator>> inserted = new();

        public TotalPlayTimeLoadPatch(OiiPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Overlays.Profile.Header.Components.TotalPlayTime", "load", MethodType.Postfix)
        {
        }

        public static void RemoveIndicators()
        {
            foreach (var reference in inserted)
            {
                if (reference.TryGetTarget(out var indicator) && indicator.Parent is FillFlowContainer flow)
                    flow.Remove(indicator, true);
            }

            inserted.Clear();
        }

        public void Postfix(TotalPlayTime __instance)
        {
            var scheduler = Reflection.GetScheduler(ClientApi.Game);
            scheduler?.AddOnce(() => insertIndicator(__instance));
        }

        private void insertIndicator(TotalPlayTime instance)
        {
            if (instance.Parent is not FillFlowContainer flow)
                return;

            var indicator = new OiiIndicator();
            indicator.User.BindTo(instance.User);
            inserted.Add(new WeakReference<OiiIndicator>(indicator));
            flow.Insert((int)flow.GetLayoutPosition(instance) + 1, indicator);

            LogInfo("total playtime indicator inserted");
        }
    }
}
