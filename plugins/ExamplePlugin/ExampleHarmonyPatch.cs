using osucc.Core;
using osucc.Plugin;
using System;

namespace ExamplePlugin
{
    /// <summary>
    /// Demonstrates a plugin-installed Harmony patch. The target is resolved by name against the
    /// runtime <c>osu.Game</c> assembly.
    /// </summary>
    public sealed class ExampleHarmonyPatch : PluginPatch<ExamplePlugin>
    {
        public ExampleHarmonyPatch(ExamplePlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.OsuGameBase", Type.EmptyTypes)
        {
        }

        /// <summary>
        /// Runs right after <c>OsuGameBase</c> is constructed — proving the plugin's patch
        /// attached before the game instance existed.
        /// </summary>
        public static void Postfix(object __instance)
            => TimingLog.Info($"OsuGameBase constructed: {__instance.GetType().Name}");
    }
}
