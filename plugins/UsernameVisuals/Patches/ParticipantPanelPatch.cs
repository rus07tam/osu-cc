using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;
using osu.Game.Users;
using osucc.Core;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Swaps the multiplayer participant panel's username sprite with a gradient copy on the
    /// first <c>updateUser()</c> and refreshes its user on every call. Panels are pooled and
    /// re-bound to different slots, so the gradient's user must be re-resolved each update; the
    /// private field is rewritten so the original <c>username.Text = ...</c> hits the gradient.
    /// </summary>
    [OsuCcPatch("osu.Game.Screens.OnlinePlay.Multiplayer.Participants.ParticipantPanel", "updateUser")]
    internal static class ParticipantPanelPatch
    {
        private static readonly FieldInfo? usernameField = Reflection.GetField("osu.Game.Screens.OnlinePlay.Multiplayer.Participants.ParticipantPanel", "username");

        private static readonly FieldInfo? currentField = Reflection.GetField("osu.Game.Screens.OnlinePlay.Multiplayer.Participants.ParticipantPanel", "current");

        private static void Postfix(object __instance)
        {
            var username = usernameField?.GetValue(__instance);

            if (username is UsernameVisualsText gradient)
            {
                gradient.User = resolvePanelUser(__instance);
                return;
            }

            if (username is not OsuSpriteText current || current.Parent is not FillFlowContainer flow)
                return;

            var replacement = UsernameVisualsText.CopyOf(current);
            replacement.User = resolvePanelUser(__instance);
            DrawableHelper.SwapInFlow(flow, current, replacement);
            usernameField!.SetValue(__instance, replacement);
        }

        private static IUser? resolvePanelUser(object instance)
        {
            var current = currentField?.GetValue(instance);
            if (current == null)
                return null;

            var slot = Reflection.GetPropertyOrField(current, "Value");
            var roomUser = slot == null ? null : Reflection.GetPropertyOrField(slot, "User");
            return roomUser == null ? null : Reflection.GetPropertyOrField(roomUser, "User") as IUser;
        }
    }
}
