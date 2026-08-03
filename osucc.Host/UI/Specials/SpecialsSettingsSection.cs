using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;
using osucc.Localisation;

namespace osucc.UI.Specials
{
    /// <summary>The "Specials" section appended to the game's settings sidebar, holding the osu!cc client's own settings.</summary>
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
        }
    }
}
