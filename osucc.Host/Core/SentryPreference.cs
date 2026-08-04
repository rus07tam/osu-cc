using osucc.Client;
using osucc.Plugin;
using System;
using System.IO;

namespace osucc.Core
{
    /// <summary>
    /// Reads the <see cref="SpecialsSetting.SentryErrorReporting"/> preference straight from the
    /// persisted ini and applies it via <c>OSU_DISABLE_ERROR_REPORTING</c> before osu constructs
    /// <c>SentryLogger</c> (which snapshots the env var once at construction, inside
    /// <c>OsuGameBase.load</c>). A prefix patch on that method guarantees ordering, so the
    /// preference takes effect on the current launch instead of the next one. Reads the file
    /// directly — no game instance or storage object exists yet at that point — and falls back to
    /// the default (disabled) when the file or key is missing.
    /// </summary>
    internal static class SentryPreference
    {
        public static void ApplyBeforeSentryLogger()
        {
            try
            {
                bool enabled = readPreference();

                // Removing the variable (null) re-enables the game's default behaviour.
                Environment.SetEnvironmentVariable("OSU_DISABLE_ERROR_REPORTING", enabled ? null : "1");
                TimingLog.Info($"Sentry preference applied before SentryLogger: OSU_DISABLE_ERROR_REPORTING={(enabled ? "cleared" : "1")}");
            }
            catch (Exception ex)
            {
                TimingLog.Error($"SentryPreference: {ex}");
            }
        }

        private static bool readPreference()
        {
            string path = Path.Combine(PluginDirectories.ResolveOsuCcDirectory(), SpecialsConfigManager.ConfigFileName);

            if (!File.Exists(path))
                return false;

            foreach (string line in File.ReadAllLines(path))
            {
                int equals = line.IndexOf('=');

                if (equals <= 0 || line[..equals].Trim() != nameof(SpecialsSetting.SentryErrorReporting))
                    continue;

                return bool.TryParse(line[(equals + 1)..].Trim(), out bool enabled) && enabled;
            }

            return false;
        }
    }
}
