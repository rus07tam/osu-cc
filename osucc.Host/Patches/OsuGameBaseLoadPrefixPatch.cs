using HarmonyLib;
using osucc.Core;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Applies the Sentry error-reporting preference before <c>OsuGameBase.Load()</c> constructs
    /// <c>SentryLogger</c>. The logger reads <c>OSU_DISABLE_ERROR_REPORTING</c> once at
    /// construction, inside the very method this patch prefixes — so the postfix
    /// (<see cref="OsuGameBaseLoadPatch"/>) fires too late for it.
    /// </summary>
    public static class OsuGameBaseLoadPrefixPatch
    {
        public static bool Install()
        {
            var load = Reflection.GetMethod("osu.Game.OsuGameBase", "Load", m => m.GetParameters().Length == 0);
            if (load == null)
            {
                TimingLog.Error("OsuGameBaseLoadPrefixPatch: OsuGameBase.Load() not found");
                return false;
            }

            HookDependencies.Main.Patch(load, prefix: Reflection.HarmonyMethod(typeof(OsuGameBaseLoadPrefixPatch), nameof(Prefix)));
            TimingLog.Info("OsuGameBase.Load patched (prefix: Sentry preference)");
            return true;
        }

        private static void Prefix()
            => SentryPreference.ApplyBeforeSentryLogger();
    }
}
