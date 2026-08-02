using HarmonyLib;
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
    /// Swaps the chat username's sprite text with a gradient copy right after construction.
    /// The readonly <c>drawableText</c> field is rewritten so the game's own setters
    /// (<c>Text</c>, <c>FontSize</c>, <c>Width</c>) keep operating on the visible gradient text.
    /// The <c>Text</c> setter is additionally postfixed so an own-username display override is
    /// re-applied after the game writes the real username.
    /// </summary>
    internal static class DrawableChatUsernamePatch
    {
        private static readonly FieldInfo? drawableTextField = Reflection.GetField("osu.Game.Overlays.Chat.DrawableChatUsername", "drawableText");

        private static readonly FieldInfo? userField = Reflection.GetField("osu.Game.Overlays.Chat.DrawableChatUsername", "user");

        public static bool Install(Harmony harmony)
        {
            bool constructor = PatchHelper.AttachConstructorPostfix(harmony, "osu.Game.Overlays.Chat.DrawableChatUsername", typeof(DrawableChatUsernamePatch), nameof(Postfix), typeof(APIUser));
            bool text = PatchHelper.AttachPostfix(harmony, "osu.Game.Overlays.Chat.DrawableChatUsername", "set_Text", typeof(DrawableChatUsernamePatch), nameof(textPostfix));
            return constructor && text;
        }

        private static void Postfix(DrawableChatUsername __instance)
        {
            if (drawableTextField?.GetValue(__instance) is not OsuSpriteText current || current is UsernameVisualsText)
                return;

            var gradient = UsernameVisualsText.CopyOf(current);
            gradient.User = userField?.GetValue(__instance) as IUser;
            drawableTextField!.SetValue(__instance, gradient);
        }

        private static void textPostfix(DrawableChatUsername __instance)
        {
            if (drawableTextField?.GetValue(__instance) is UsernameVisualsText gradient)
                gradient.ReapplyDisplay();
        }
    }
}
