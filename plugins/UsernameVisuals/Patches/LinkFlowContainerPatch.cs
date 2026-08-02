using HarmonyLib;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Containers;
using osu.Game.Localisation;
using osu.Game.Online.Chat;
using osu.Game.Users;
using osucc.Core;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Reroutes <c>LinkFlowContainer.AddUserLink()</c> through a gradient text while keeping the
    /// clickable profile-link behaviour, so usernames embedded in text flows render as
    /// gradients. Uses the public <c>AddLink(IEnumerable&lt;SpriteText&gt;, ...)</c> overload,
    /// which internally builds the same <c>createLink</c> link container as the original.
    /// </summary>
    internal static class LinkFlowContainerPatch
    {
        private static readonly Lazy<MethodInfo?> applyDefaultParametersMethod = new(() =>
            typeof(TextFlowContainer).GetMethod("ApplyDefaultCreationParameters", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

        public static bool Install(Harmony harmony)
            => PatchHelper.AttachPrefix(harmony, "osu.Game.Graphics.Containers.LinkFlowContainer", "AddUserLink", typeof(LinkFlowContainerPatch), nameof(Prefix));

        private static bool Prefix(LinkFlowContainer __instance, IUser user, Action<SpriteText>? creationParameters)
        {
            try
            {
                var gradient = new UsernameVisualsText
                {
                    User = user,
                    Text = user.Username,
                };

                applyDefaultParametersMethod.Value?.Invoke(__instance, new object[] { gradient });
                creationParameters?.Invoke(gradient);

                __instance.AddLink(new[] { gradient }, LinkAction.OpenUserProfile, user, ContextMenuStrings.ViewProfile.ToString());
                return false;
            }
            catch
            {
                // fall back to the original implementation on any reflection failure
                return true;
            }
        }
    }
}
