using osu.Game.Overlays.Settings;
using osucc.Core;
using osucc.UI.Specials;
using System.Collections.Generic;

namespace osucc.Patches
{
    /// <summary>
    /// Appends the "Specials" section to the settings sidebar. Targets the virtual
    /// <c>SettingsOverlay.CreateSections()</c>.
    /// </summary>
    public sealed class SettingsOverlayCreateSectionsPatch : OsuCcPatch
    {
        public SettingsOverlayCreateSectionsPatch()
            : base("osu.Game.Overlays.SettingsOverlay", "CreateSections", MethodType.Postfix)
        {
        }

        public void Postfix(ref IEnumerable<SettingsSection> __result)
        {
            if (__result is ICollection<SettingsSection> sections)
            {
                sections.Add(new SpecialsSettingsSection());
                LogInfo("Specials settings section added");
            }
            else
            {
                LogError("CreateSections result is not a collection; Specials section not added");
            }
        }
    }
}
