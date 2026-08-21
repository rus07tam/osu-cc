using osu.Framework.Graphics.Sprites;
using osucc.Core;
using osucc.Plugin;

namespace UsernameVisuals
{
    /// <summary>
    /// Re-applies the own-username display whenever the game itself writes the text of a
    /// <see cref="UsernameVisualsText"/>.
    /// </summary>
    public sealed class SpriteTextSetTextPatch : PluginPatch<UsernameVisualsPlugin>
    {
        public SpriteTextSetTextPatch(UsernameVisualsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, typeof(SpriteText), "set_Text", MethodType.Postfix)
        {
        }

        public static void Postfix(SpriteText __instance)
        {
            if (__instance is UsernameVisualsText gradient)
                gradient.ReapplyDisplay();
        }
    }
}
