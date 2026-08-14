using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps.Timing;
using osu.Game.Screens.Play;
using osucc.Client;
using osucc.Core;
using System.Collections.Generic;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Watches breaks during gameplay and dynamically creates/removes
    /// <see cref="SkipOverlay"/> instances so the player can skip long breaks.
    /// Added to <see cref="GameplayClockContainer"/>'s children by <see cref="PlayerBreakSkipPatch"/>.
    /// </summary>
    public partial class OsuCcBreakSkipper : Component
    {
        /// <summary>Arrive this many ms before break ends to give the player time to react.</summary>
        private const double skip_lead_in = 2000;

        /// <summary>Don't offer skip when remaining savings are less than this.</summary>
        private const double minimum_skip_savings = 1000;

        private readonly Player player;
        private readonly GameplayClockContainer clockContainer;
        private readonly IReadOnlyList<BreakPeriod> breaks;

        private SkipOverlay? currentOverlay;
        private BreakPeriod? activeBreak;

        // Cached reflection handles for the seek sequence
        private object? drawableRuleset;
        private PropertyInfo? frameStablePlaybackProp;
        private FieldInfo? samplePlaybackDisabledField;
        private MethodInfo? updateSampleDisabledStateMethod;

        public OsuCcBreakSkipper(Player player, GameplayClockContainer clockContainer, IReadOnlyList<BreakPeriod> breaks)
        {
            this.player = player;
            this.clockContainer = clockContainer;
            this.breaks = breaks;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Resolve private/protected members needed for a clean seek
            drawableRuleset = findProperty(player.GetType(), "DrawableRuleset")?.GetValue(player);
            if (drawableRuleset != null)
                frameStablePlaybackProp = drawableRuleset.GetType().GetProperty("FrameStablePlayback",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            samplePlaybackDisabledField = Reflection.FindField(player.GetType(), "samplePlaybackDisabled");
            updateSampleDisabledStateMethod = findMethod(player.GetType(), "updateSampleDisabledState");

            ClientConfig.SkipBreakTime.BindValueChanged(e =>
            {
                if (!e.NewValue)
                    removeOverlay();
            });
        }

        protected override void Update()
        {
            base.Update();

            if (!ClientConfig.SkipBreakTime.Value)
                return;

            double currentTime = clockContainer.CurrentTime;

            var brk = findBreakAt(currentTime);

            if (brk != null)
            {
                if (currentOverlay == null || !ReferenceEquals(activeBreak, brk))
                {
                    removeOverlay();
                    tryCreateOverlay(brk, currentTime);
                }
            }
            else
            {
                removeOverlay();
            }
        }

        private BreakPeriod? findBreakAt(double time)
        {
            foreach (var b in breaks)
            {
                if (b.HasEffect && time >= b.StartTime && time < b.EndTime)
                    return b;
            }

            return null;
        }

        private void tryCreateOverlay(BreakPeriod brk, double currentTime)
        {
            double breakEnd = brk.EndTime;
            double skipTarget = breakEnd - skip_lead_in;

            if (skipTarget - currentTime < minimum_skip_savings)
                return;

            activeBreak = brk;

            currentOverlay = new SkipOverlay(breakEnd)
            {
                RequestSkip = () =>
                {
                    performBreakSkip(skipTarget);
                    removeOverlay();
                }
            };

            clockContainer.Add(currentOverlay);
            TimingLog.Info($"Break skip overlay created (break {brk.StartTime:F0}→{brk.EndTime:F0}, skip→{skipTarget:F0})");
        }

        /// <summary>
        /// Performs a seek that mirrors what <c>Player.PerformIntroSkip</c> and
        /// <c>Player.SetGameplayStartTime</c> do:
        /// 1. Mute sample playback so stale hitsounds don't fire
        /// 2. Temporarily disable FrameStablePlayback to avoid frame-by-frame catch-up
        /// 3. Seek the clock
        /// 4. Re-enable FrameStablePlayback after one frame
        /// 5. Restore sample playback state
        /// </summary>
        private void performBreakSkip(double targetTime)
        {
            try
            {
                // 1. Mute samples (like PerformIntroSkip does)
                var sampleDisabled = samplePlaybackDisabledField?.GetValue(player) as Bindable<bool>;
                if (sampleDisabled != null)
                    sampleDisabled.Value = true;

                // 2. Temporarily disable frame-stable playback so FrameStabilityContainer
                //    doesn't try to process every intermediate frame
                bool wasFrameStable = false;
                if (drawableRuleset != null && frameStablePlaybackProp != null)
                {
                    wasFrameStable = (bool)(frameStablePlaybackProp.GetValue(drawableRuleset) ?? true);
                    frameStablePlaybackProp.SetValue(drawableRuleset, false);
                }

                // 3. Seek
                clockContainer.Seek(targetTime);

                // 4. Re-enable frame-stable playback after one frame
                if (drawableRuleset != null && frameStablePlaybackProp != null && wasFrameStable)
                {
                    var capturedProp = frameStablePlaybackProp;
                    var capturedRuleset = drawableRuleset;
                    Scheduler.AddDelayed(() => capturedProp.SetValue(capturedRuleset, true), 0);
                }

                // 5. Restore sample playback to whatever the beatmap state dictates
                updateSampleDisabledStateMethod?.Invoke(player, null);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"OsuCcBreakSkipper.performBreakSkip: {ex}");
            }
        }

        private void removeOverlay()
        {
            if (currentOverlay != null)
            {
                currentOverlay.Expire();
                currentOverlay = null;
                activeBreak = null;
            }
        }

        private static PropertyInfo? findProperty(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var prop = t.GetProperty(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (prop != null) return prop;
            }

            return null;
        }

        private static MethodInfo? findMethod(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var method = t.GetMethod(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (method != null) return method;
            }

            return null;
        }
    }
}
