using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;
using osucc.Core;
using osucc.Localisation;
using osucc.Plugin;

namespace osucc.UI.Specials
{
    /// <summary>The "Specials" section appended to the game's settings sidebar, followed by any plugin-registered subsections.</summary>
    public partial class SpecialsSettingsSection : SettingsSection
    {
        public override LocalisableString Header => SpecialsSettingsStrings.SectionHeader;

        public override Drawable CreateIcon() => new SpriteIcon
        {
            Icon = FontAwesome.Solid.Star
        };

        public SpecialsSettingsSection()
        {
            Add(new SpecialsSettingsSubsection());

            foreach (var factory in PluginManager.SettingsSubsectionFactories)
            {
                try
                {
                    var subsection = factory();

                    if (subsection != null)
                        Add(subsection);
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"SpecialsSettingsSection: failed to create plugin settings subsection: {ex}");
                }
            }
        }
    }
}
