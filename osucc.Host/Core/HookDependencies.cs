using HarmonyLib;
using System.Reflection;

namespace osucc.Core
{
    /// <summary>
    /// Ensures the hook's own runtime dependencies (<c>0Harmony.dll</c>, <c>SharpCompress.dll</c>)
    /// load from this hook's directory before any of their types are touched. The hook DLL lives
    /// outside the app base (osu-cc data root, not the install dir), so the default resolver would
    /// not find them there; osu.* itself resolves from the production app base at runtime.
    /// </summary>
    public static class HookDependencies
    {
        private static readonly object lockObject = new();

        private static bool loaded;

        public static void EnsureLoaded()
        {
            if (loaded)
                return;

            lock (lockObject)
            {
                if (loaded)
                    return;

                string dir = Path.GetDirectoryName(typeof(HookDependencies).Assembly.Location)!;

                foreach (string dependency in new[] { "0Harmony.dll", "SharpCompress.dll" })
                {
                    string path = Path.Combine(dir, dependency);

                    if (File.Exists(path))
                    {
                        Assembly.LoadFrom(path);
                        TimingLog.Info($"{dependency} loaded from {path}");
                    }
                    else
                    {
                        TimingLog.Error($"{dependency} not found at {path}; relying on default resolution");
                    }
                }

                loaded = true;
            }
        }

        public static Harmony Create(string id)
        {
            EnsureLoaded();
            return new Harmony(id);
        }

        /// <summary>
        /// The single Harmony instance shared by every built-in patch, so the client's whole
        /// surface unpatchable via <c>UnpatchAll("osucc")</c>. Plugins keep their own scoped
        /// instances via <see cref="Create"/>.
        /// </summary>
        public static Harmony Main
        {
            get
            {
                EnsureLoaded();
                return main ??= new Harmony("osucc");
            }
        }

        private static Harmony? main;
    }
}
