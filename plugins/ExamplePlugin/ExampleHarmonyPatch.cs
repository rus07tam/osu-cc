using osucc.Core;
using osucc.Plugin;
using System;

namespace ExamplePlugin
{
    /// <summary>
    /// Demonstrates a plugin-installed Harmony patch. Targets are resolved by name against the
    /// runtime <c>osu.Game</c> assembly, following the same convention the osu!cc client itself
    /// uses — so this stays correct regardless of the production build. The patch is applied
    /// through the host, which tracks it and reverts it on live disable.
    /// </summary>
    public static class ExampleHarmonyPatch
    {
        private static IOsuCcPluginHost host = null!;

        public static IDisposable? Install(IOsuCcPluginHost host)
        {
            ExampleHarmonyPatch.host = host;
            return PatchHelper.AttachConstructorPostfix(host, "osu.Game.OsuGameBase", typeof(ExampleHarmonyPatch), nameof(Postfix));
        }

        /// <summary>
        /// Runs right after <c>OsuGameBase</c> is constructed — proving the plugin's patch
        /// attached before the game instance existed.
        /// </summary>
        private static void Postfix(object __instance)
            => host.Log(LogLevel.Info, $"OsuGameBase constructed: {__instance.GetType().Name}");
    }
}
