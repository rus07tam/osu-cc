using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Users;
using osucc.Core;
using osucc.Plugin;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Swaps the toolbar user button's username text with a gradient copy, and rewrites the private
    /// field so the scheduled <c>userChanged</c> keeps updating the visible text when the local user changes.
    /// </summary>
    public sealed class ToolbarUserButtonPatch : PluginPatch<UsernameVisualsPlugin>
    {
        private static readonly FieldInfo? usernameTextField = Reflection.GetField("osu.Game.Overlays.Toolbar.ToolbarUserButton", "usernameText");
        private static readonly FieldInfo? localUserField = Reflection.GetField("osu.Game.Overlays.Toolbar.ToolbarUserButton", "localUser");

        public ToolbarUserButtonPatch(UsernameVisualsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Overlays.Toolbar.ToolbarUserButton", "load", MethodType.Postfix)
        {
        }

        public static void Postfix(object __instance)
        {
            if (usernameTextField?.GetValue(__instance) is not OsuSpriteText current || current is UsernameVisualsText)
                return;

            if (current.Parent == null)
                return;

            var gradient = UsernameVisualsText.CopyOf(current);
            gradient.User = resolveLocalUser(__instance);
            gradient.TrackLocalUser = true;
            DrawableHelper.SwapInParent(current, gradient);
            usernameTextField.SetValue(__instance, gradient);
        }

        private static IUser? resolveLocalUser(object instance)
        {
            if (localUserField?.GetValue(instance) is not { } bindable)
                return null;

            return Reflection.GetPropertyOrField(bindable, "Value") as IUser;
        }
    }
}
