using osu.Framework.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Mods;
using osucc.Client;
using osucc.Core;
using osucc.Localisation;

namespace osucc.UI.Mods
{
    /// <summary>
    /// A footer button which selects a random set of valid mods via
    /// <see cref="ClientMods.ApplyRandomMods"/>. Mirrors the game's <c>DeselectAllModsButton</c>.
    /// </summary>
    public partial class RandomModsButton : ShearedButton
    {
        public RandomModsButton(ModSelectOverlay overlay)
        {
            Width = ModSelectOverlay.BUTTON_WIDTH;

            Text = ModsStrings.RandomModsButton;
            Action = () => ClientMods.ApplyRandomMods(overlay);
        }
    }
}
