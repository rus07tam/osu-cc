using osu.Game.Graphics.Sprites;
using osu.Game.Users;
using osucc.Core;
using osucc.Plugin;

namespace UsernameVisuals
{
    /// <summary>
    /// Replaces the username sprite created by <c>UserPanel.CreateUsername()</c> with a gradient text.
    /// </summary>
    public sealed class UserPanelPatch : PluginPatch<UsernameVisualsPlugin>
    {
        public UserPanelPatch(UsernameVisualsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Users.UserPanel", "CreateUsername", MethodType.Postfix)
        {
        }

        public static void Postfix(UserPanel __instance, ref OsuSpriteText __result)
        {
            if (__result == null || __result is UsernameVisualsText)
                return;

            var gradient = UsernameVisualsText.CopyOf(__result);
            gradient.User = __instance.User;
            __result = gradient;
        }
    }
}
