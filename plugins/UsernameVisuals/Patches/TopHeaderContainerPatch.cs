using osu.Game.Graphics.Sprites;
using osu.Game.Users;
using osucc.Core;
using osucc.Plugin;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Swaps the profile header's username text with a gradient copy each time the displayed user is updated.
    /// </summary>
    public sealed class TopHeaderContainerPatch : PluginPatch<UsernameVisualsPlugin>
    {
        private static readonly FieldInfo? usernameTextField = Reflection.GetField("osu.Game.Overlays.Profile.Header.TopHeaderContainer", "usernameText");

        public TopHeaderContainerPatch(UsernameVisualsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Overlays.Profile.Header.TopHeaderContainer", "updateUser", MethodType.Postfix)
        {
        }

        public static void Postfix(object __instance)
        {
            var current = usernameTextField?.GetValue(__instance);

            if (current is UsernameVisualsText gradient)
            {
                gradient.User = resolveProfileUser(__instance);
                return;
            }

            if (current is not OsuSpriteText text)
                return;

            var replacement = UsernameVisualsText.CopyOf(text);
            replacement.User = resolveProfileUser(__instance);
            DrawableHelper.SwapInParent(text, replacement);
            usernameTextField!.SetValue(__instance, replacement);
        }

        private static IUser? resolveProfileUser(object instance)
        {
            var bindable = Reflection.GetPropertyOrField(instance, "User");
            if (bindable == null)
                return null;

            var data = Reflection.GetPropertyOrField(bindable, "Value");
            return data == null ? null : Reflection.GetPropertyOrField(data, "User") as IUser;
        }
    }
}
