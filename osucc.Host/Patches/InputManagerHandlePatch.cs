using HarmonyLib;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osucc.Core;
using System;
using System.Reflection;

namespace osucc.Patches
{
    public static class InputManagerHandlePatch
    {
        public static event Action<UIEvent>? OnInputEvent;

        private static readonly System.Collections.Generic.Dictionary<osuTK.Input.Key, int> lastKeyPressTimes = new System.Collections.Generic.Dictionary<osuTK.Input.Key, int>();

        public static bool Install()
        {
            var method = typeof(osu.Framework.Graphics.Drawable).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(KeyDownEvent) }, null);

            if (method == null)
                return false;

            HookDependencies.Main.Patch(method, prefix: Reflection.HarmonyMethod(typeof(InputManagerHandlePatch), nameof(Prefix)));
            return true;
        }

        private static void Prefix(osu.Framework.Graphics.Drawable __instance, KeyDownEvent e)
        {
            try
            {
                if (e != null && !e.Repeat)
                {
                    int now = Environment.TickCount;
                    if (lastKeyPressTimes.TryGetValue(e.Key, out int lastTime))
                    {
                        // 10ms threshold to deduplicate the same event bubbling through multiple drawables
                        if (now - lastTime < 10)
                            return;
                    }

                    lastKeyPressTimes[e.Key] = now;
                    OnInputEvent?.Invoke(e);
                }
            }
            catch
            {
            }
        }
    }
}
