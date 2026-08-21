using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osucc.Localisation;
using osucc.Plugin;
using System;

namespace osucc.UI.Plugins
{
    /// <summary>
    /// A clickable plugin name. Clicking opens the plugin's details card
    /// (<see cref="PluginDetailsOverlay"/>).
    /// </summary>
    public partial class PluginNameLink : ClickableContainer
    {
        public static Action<string>? ShowDetailsHandler { get; set; }

        public static Action<PluginEntry>? ShowDetailsEntryHandler { get; set; }

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private readonly OsuSpriteText text;

        /// <summary>Localised name rendered by this link.</summary>
        public LocalisableString Text
        {
            set => text.Text = value;
        }

        public PluginNameLink(string pluginId, LocalisableString fallbackName, float fontSize = 13, FontWeight weight = FontWeight.Medium)
        {
            AutoSizeAxes = Axes.Both;
            Action = () => ShowDetailsHandler?.Invoke(pluginId);

            Child = text = new OsuSpriteText
            {
                Text = OsuCcLocalisation.Get($"{pluginId}:name", fallbackName.ToString()),
                Font = OsuFont.GetFont(size: fontSize, weight: weight),
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            text.Colour = colourProvider.Content1;
        }

        protected override bool OnHover(HoverEvent e)
        {
            text.FadeColour(colourProvider.Light1, 100);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            text.FadeColour(colourProvider.Content1, 100);
            base.OnHoverLost(e);
        }
    }
}
