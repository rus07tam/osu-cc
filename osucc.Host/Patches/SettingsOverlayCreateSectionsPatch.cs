using HarmonyLib;
using osu.Game.Overlays.Settings;
using osucc.Core;
using osucc.UI.Specials;
using System.Reflection;

namespace osucc.Patches
{
    /// <summary>
    /// Appends the "Specials" section to the settings sidebar. Targets the virtual
    /// <c>SettingsOverlay.CreateSections()</c>.
    /// </summary>
    public static class SettingsOverlayCreateSectionsPatch
    {
        public static bool Install()
        {
            var method = Reflection.GetGameType("osu.Game.Overlays.SettingsOverlay")?.GetMethod("CreateSections", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                TimingLog.Error("SettingsOverlayCreateSectionsPatch: CreateSections method not found");
                return false;
            }

            HookDependencies.Main.Patch(method, postfix: Reflection.HarmonyMethod(typeof(SettingsOverlayCreateSectionsPatch), nameof(Postfix)));
            TimingLog.Info("SettingsOverlay.CreateSections patched (postfix)");
            return true;
        }

        private static void Postfix(ref IEnumerable<SettingsSection> __result)
        {
            if (__result is ICollection<SettingsSection> sections)
            {
                sections.Add(new SpecialsSettingsSection());
                TimingLog.Info("Specials settings section added");
            }
            else
            {
                TimingLog.Error("CreateSections result is not a collection; Specials section not added");
            }
        }
    }
}
