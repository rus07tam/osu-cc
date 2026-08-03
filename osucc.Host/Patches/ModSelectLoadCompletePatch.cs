using HarmonyLib;
using osu.Game.Overlays.Mods;
using osucc.Client;
using osucc.Core;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Tracks live mod-select overlays so the Specials toggles react immediately. Targets
    /// <c>ModSelectOverlay.LoadComplete()</c>; the postfix registers the overlay with
    /// <see cref="ClientMods"/>.
    /// </summary>
    public static class ModSelectLoadCompletePatch
    {
        public static bool Install()
        {
            var method = Reflection.GetGameType("osu.Game.Overlays.Mods.ModSelectOverlay")?.GetMethod("LoadComplete", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                TimingLog.Error("ModSelectLoadCompletePatch: LoadComplete method not found");
                return false;
            }

            HookDependencies.Main.Patch(method, postfix: Reflection.HarmonyMethod(typeof(ModSelectLoadCompletePatch), nameof(Postfix)));
            TimingLog.Info("ModSelectOverlay.LoadComplete patched (postfix)");
            return true;
        }

        private static void Postfix(ModSelectOverlay __instance)
        {
            try
            {
                ClientMods.Register(__instance);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"ModSelectLoadCompletePatch.Postfix: {ex}");
            }
        }
    }
}
