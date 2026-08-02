using HarmonyLib;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Users;
using osucc.Core;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Replaces the sprite text child of <c>ClickableUsername</c> with a gradient copy after the
    /// constructor builds it. <c>ClickableUsername</c> is internal, so the instance and the
    /// private <c>user</c> field are handled reflectively.
    /// </summary>
    internal static class ClickableUsernamePatch
    {
        private static readonly FieldInfo? userField = Reflection.GetField("osu.Game.Users.Drawables.ClickableUsername", "user");

        public static bool Install(Harmony harmony)
            => PatchHelper.AttachConstructorPostfix(harmony, "osu.Game.Users.Drawables.ClickableUsername", typeof(ClickableUsernamePatch), nameof(Postfix), typeof(APIUser));

        private static void Postfix(object __instance)
        {
            if (Reflection.GetPropertyOrField(__instance, "Child") is not OsuSpriteText current || current is UsernameVisualsText)
                return;

            var gradient = UsernameVisualsText.CopyOf(current);
            gradient.User = userField?.GetValue(__instance) as IUser;
            Reflection.SetPropertyOrField(__instance, "Child", gradient);
        }
    }
}
