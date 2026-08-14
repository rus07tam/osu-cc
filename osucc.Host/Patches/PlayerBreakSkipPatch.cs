using osu.Game.Screens.Play;
using osucc.Core;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Patches <c>Player.LoadComplete</c> to inject an <see cref="OsuCcBreakSkipper"/>
    /// that shows a SKIP button during mid-map break periods.
    /// Uses only standard ppy.osu.Game APIs — no torii-specific types required.
    /// </summary>
    public static class PlayerBreakSkipPatch
    {
        public static bool Install()
        {
            var playerType = Reflection.GetGameType("osu.Game.Screens.Play.Player");

            if (playerType == null)
            {
                TimingLog.Error("PlayerBreakSkipPatch: Player type not found");
                return false;
            }

            var method = findMethod(playerType, "LoadComplete");

            if (method == null)
            {
                TimingLog.Error("PlayerBreakSkipPatch: Player.LoadComplete not found");
                return false;
            }

            HookDependencies.Main.Patch(method,
                postfix: Reflection.HarmonyMethod(typeof(PlayerBreakSkipPatch), nameof(Postfix)));
            TimingLog.Info("Player.LoadComplete patched for break skipping (postfix)");
            return true;
        }

        private static void Postfix(Player __instance)
        {
            try
            {
                var breaks = __instance.GameplayState.Beatmap.Breaks;

                if (breaks == null || breaks.Count == 0)
                {
                    TimingLog.Info("PlayerBreakSkipPatch: no breaks in beatmap");
                    return;
                }

                // Read the protected GameplayClockContainer property.
                var gccProp = findProperty(__instance.GetType(), "GameplayClockContainer");
                var gcc = gccProp?.GetValue(__instance) as GameplayClockContainer;

                if (gcc == null)
                {
                    TimingLog.Error("PlayerBreakSkipPatch: GameplayClockContainer not found");
                    return;
                }

                // Add the skipper as a child of GameplayClockContainer so it
                // inherits the gameplay clock and has access to the DI container
                // that provides IGameplayClock (needed by SkipOverlay).
                var skipper = new OsuCcBreakSkipper(__instance, gcc, breaks);
                gcc.Add(skipper);
                TimingLog.Info($"OsuCcBreakSkipper injected into GameplayClockContainer ({breaks.Count} break(s))");
            }
            catch (Exception ex)
            {
                TimingLog.Error($"PlayerBreakSkipPatch.Postfix: {ex}");
            }
        }

        private static MethodInfo? findMethod(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var method = t.GetMethod(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (method != null)
                    return method;
            }

            return null;
        }

        private static PropertyInfo? findProperty(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var prop = t.GetProperty(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (prop != null)
                    return prop;
            }

            return null;
        }
    }
}
