using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Users;
using osucc.Core;
using osucc.Plugin;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Swaps the multiplayer participant panel's username sprite with a gradient copy on the
    /// first <c>updateUser()</c> and refreshes its user on every call.
    /// </summary>
    public sealed class ParticipantPanelPatch : PluginPatch<UsernameVisualsPlugin>
    {
        private static readonly FieldInfo? usernameField = Reflection.GetField("osu.Game.Screens.OnlinePlay.Multiplayer.Participants.ParticipantPanel", "username");
        private static readonly FieldInfo? currentField = Reflection.GetField("osu.Game.Screens.OnlinePlay.Multiplayer.Participants.ParticipantPanel", "current");

        public ParticipantPanelPatch(UsernameVisualsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Screens.OnlinePlay.Multiplayer.Participants.ParticipantPanel", "updateUser", MethodType.Postfix)
        {
        }

        public static void Postfix(object __instance)
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
