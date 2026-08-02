using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;
using osucc.Plugin;

namespace SubdivideNations
{
    /// <summary>Settings subsection injected into the "Specials" section: master toggle plus flag images.</summary>
    public partial class SubdivideNationsSettingsSubsection : SettingsSubsection
    {
        protected override LocalisableString Header => SubdivideNationsStrings.Name;

        public SubdivideNationsSettingsSubsection(PluginSettings settings)
        {
            this.AddCheckbox(settings, "subdivide_enabled", true, SubdivideNationsStrings.ShowRegionsCaption, SubdivideNationsStrings.ShowRegionsHint);
            this.AddCheckbox(settings, "show_flags", true, SubdivideNationsStrings.ShowFlagsCaption, SubdivideNationsStrings.ShowFlagsHint);
        }
    }
}
