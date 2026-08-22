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
    /// Patches chat usernames right after construction. The readonly <c>DrawableText</c> field is
    /// rewritten with a gradient copy.
    /// </summary>
    public sealed class DrawableChatUsernamePatch : PluginPatch<UsernameVisualsPlugin>
    {
        public static class Fields
        {
            public static readonly FieldInfo? DrawableText = Reflection.GetField("osu.Game.Overlays.Chat.DrawableChatUsername", "drawableText");
            public static readonly FieldInfo? User = Reflection.GetField("osu.Game.Overlays.Chat.DrawableChatUsername", "user");
        }

        public DrawableChatUsernamePatch(UsernameVisualsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Overlays.Chat.DrawableChatUsername", new[] { typeof(APIUser) })
        {
        }

        public static void Postfix(DrawableChatUsername __instance)
        {
            if (Fields.DrawableText?.GetValue(__instance) is not OsuSpriteText current || current is UsernameVisualsText)
                return;

            var gradient = UsernameVisualsText.CopyOf(current);
            gradient.User = Fields.User?.GetValue(__instance) as IUser;
            Fields.DrawableText!.SetValue(__instance, gradient);
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
            if (DrawableChatUsernamePatch.Fields.DrawableText?.GetValue(__instance) is UsernameVisualsText gradient)
                gradient.ReapplyDisplay();
        }
    }
}
