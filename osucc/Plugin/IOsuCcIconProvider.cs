using osu.Framework.Graphics.Sprites;

namespace osucc.Plugin
{
    /// <summary>
    /// Optional interface a plugin can implement to supply a FontAwesome icon for its card in the
    /// plugins overlay. Takes precedence over any <c>icon.*</c> file and over
    /// <see cref="OsuCcPluginAttribute.IconResource"/>.
    /// </summary>
    public interface IOsuCcIconProvider
    {
        /// <summary>The icon to display, or <c>null</c> for none.</summary>
        IconUsage? Icon { get; }
    }
}
