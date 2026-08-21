using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Users;
using osucc.Core;
using osucc.Plugin;
using System;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Replaces the sprite text child of <c>ClickableUsername</c> with a gradient copy after the
    /// constructor builds it.
    /// </summary>
    public sealed class ClickableUsernamePatch : PluginPatch<UsernameVisualsPlugin>
    {
        private static readonly FieldInfo? userField = Reflection.GetField("osu.Game.Users.Drawables.ClickableUsername", "user");

        public ClickableUsernamePatch(UsernameVisualsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Users.Drawables.ClickableUsername", new[] { typeof(APIUser) })
        {
        }

        public static void Postfix(object __instance)
        {
            if (Reflection.GetPropertyOrField(__instance, "Child") is not OsuSpriteText current || current is UsernameVisualsText)
                return;

            var gradient = UsernameVisualsText.CopyOf(current);
            gradient.User = userField?.GetValue(__instance) as IUser;
            Reflection.SetPropertyOrField(__instance, "Child", gradient);
        }
    }
}
