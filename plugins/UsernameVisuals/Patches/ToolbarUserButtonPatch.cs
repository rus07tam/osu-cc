using HarmonyLib;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Users;
using osucc.Core;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Swaps the toolbar user button's username text (bottom-left toolbar, always the local
    /// user) with a gradient copy, and rewrites the private field so the scheduled
    /// <c>userChanged</c> keeps updating the visible text when the local user changes. The swap
    /// can run before the local user is available, so the gradient text tracks
    /// <see cref="IAPIProvider.LocalUser"/> itself instead of snapshotting it once.
    /// </summary>
    internal static class ToolbarUserButtonPatch
    {
        private static readonly FieldInfo? usernameTextField = Reflection.GetField("osu.Game.Overlays.Toolbar.ToolbarUserButton", "usernameText");

        private static readonly FieldInfo? localUserField = Reflection.GetField("osu.Game.Overlays.Toolbar.ToolbarUserButton", "localUser");

        public static bool Install(Harmony harmony)
            => PatchHelper.AttachPostfix(harmony, "osu.Game.Overlays.Toolbar.ToolbarUserButton", "load", typeof(ToolbarUserButtonPatch), nameof(Postfix));

        private static void Postfix(object __instance)
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
