using osu.Game.Screens.Play;
using osucc.Core;
using System;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Patches <c>Player.LoadComplete</c> to inject an <see cref="OsuCcBreakSkipper"/>
    /// that shows a SKIP button during mid-map break periods.
    /// </summary>
    public sealed class PlayerBreakSkipPatch : OsuCcPatch
    {
        public PlayerBreakSkipPatch()
            : base("osu.Game.Screens.Play.Player", "LoadComplete", MethodType.Postfix)
        {
        }

        public void Postfix(Player __instance)
        {
            var breaks = __instance.GameplayState.Beatmap.Breaks;

            if (breaks == null || breaks.Count == 0)
            {
                LogInfo("no breaks in beatmap");
                return;
            }

            // Read the protected GameplayClockContainer property.
            var gccProp = findProperty(__instance.GetType(), "GameplayClockContainer");
            var gcc = gccProp?.GetValue(__instance) as GameplayClockContainer;

            if (gcc == null)
            {
                LogError("GameplayClockContainer not found");
                return;
            }

            var skipper = new OsuCcBreakSkipper(__instance, gcc, breaks);
            gcc.Add(skipper);
            LogInfo($"OsuCcBreakSkipper injected into GameplayClockContainer ({breaks.Count} break(s))");
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
