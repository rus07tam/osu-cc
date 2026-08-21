using osu.Framework.Input.Events;
using osucc.Core;
using System;
using System.Collections.Generic;

namespace osucc.Patches
{
    public sealed class InputManagerHandlePatch : OsuCcPatch
    {
        public static event Action<UIEvent>? OnInputEvent;

        private static readonly Dictionary<osuTK.Input.Key, int> lastKeyPressTimes = new();

        public InputManagerHandlePatch()
            : base(typeof(osu.Framework.Graphics.Drawable), "OnKeyDown", m =>
            {
                var p = m.GetParameters();
                return p.Length == 1 && p[0].ParameterType == typeof(KeyDownEvent);
            }, MethodType.Prefix)
        {
        }

        public static void Prefix(osu.Framework.Graphics.Drawable __instance, KeyDownEvent e)
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
    }
}
