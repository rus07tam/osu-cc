using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.Chat;
using osu.Game.Users;
using osucc.Core;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Patches chat usernames right after construction. The readonly <c>drawableText</c> field is
    /// rewritten with a gradient copy, so the game's own setters (<c>Text</c>, <c>FontSize</c>,
    /// <c>Width</c>) keep operating on the visible gradient text. The <c>Text</c> setter is
    /// additionally postfixed by <see cref="DrawableChatUsernameTextPatch"/> so an own-username
    /// display override is re-applied after the game writes the real username.
    /// </summary>
    [OsuCcConstructorPatch("osu.Game.Overlays.Chat.DrawableChatUsername", typeof(APIUser))]
    internal static class DrawableChatUsernamePatch
    {
        /// <summary>Shared reflective field lookups for <see cref="DrawableChatUsername"/>.</summary>
        internal static class Fields
        {
            internal static readonly FieldInfo? drawableText = Reflection.GetField("osu.Game.Overlays.Chat.DrawableChatUsername", "drawableText");

            internal static readonly FieldInfo? user = Reflection.GetField("osu.Game.Overlays.Chat.DrawableChatUsername", "user");
        }

        private static void Postfix(DrawableChatUsername __instance)
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
    [OsuCcPatch("osu.Game.Overlays.Chat.DrawableChatUsername", "set_Text")]
    internal static class DrawableChatUsernameTextPatch
    {
        private static void Postfix(DrawableChatUsername __instance)
        {
            if (DrawableChatUsernamePatch.Fields.drawableText?.GetValue(__instance) is UsernameVisualsText gradient)
                gradient.ReapplyDisplay();
        }
    }
}
