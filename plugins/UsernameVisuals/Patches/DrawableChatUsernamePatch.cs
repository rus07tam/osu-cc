using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.Chat;
using osu.Game.Users;
using osucc.Core;
using osucc.Plugin;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Patches chat usernames right after construction. The readonly <c>drawableText</c> field is
    /// rewritten with a gradient copy.
    /// </summary>
    public sealed class DrawableChatUsernamePatch : PluginPatch<UsernameVisualsPlugin>
    {
        public static class Fields
        {
            public static readonly FieldInfo? drawableText = Reflection.GetField("osu.Game.Overlays.Chat.DrawableChatUsername", "drawableText");
            public static readonly FieldInfo? user = Reflection.GetField("osu.Game.Overlays.Chat.DrawableChatUsername", "user");
        }

        public DrawableChatUsernamePatch(UsernameVisualsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Overlays.Chat.DrawableChatUsername", new[] { typeof(APIUser) })
        {
        }

        public static void Postfix(DrawableChatUsername __instance)
        {
            if (Fields.drawableText?.GetValue(__instance) is not OsuSpriteText current || current is UsernameVisualsText)
                return;

            var gradient = UsernameVisualsText.CopyOf(current);
            gradient.User = Fields.user?.GetValue(__instance) as IUser;
            Fields.drawableText!.SetValue(__instance, gradient);
        }
    }

    /// <summary>
    /// Re-applies an own-username display override when the game writes the real username into a
    /// chat name, on the gradient copy installed by <see cref="DrawableChatUsernamePatch"/>.
    /// </summary>
    public sealed class DrawableChatUsernameTextPatch : PluginPatch<UsernameVisualsPlugin>
    {
        public DrawableChatUsernameTextPatch(UsernameVisualsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Overlays.Chat.DrawableChatUsername", "set_Text", MethodType.Postfix)
        {
        }

        public static void Postfix(DrawableChatUsername __instance)
        {
            if (DrawableChatUsernamePatch.Fields.drawableText?.GetValue(__instance) is UsernameVisualsText gradient)
                gradient.ReapplyDisplay();
        }
    }
}
