using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Containers;
using osu.Game.Localisation;
using osu.Game.Online.Chat;
using osu.Game.Users;
using osucc.Core;
using osucc.Plugin;
using System;
using System.Reflection;

namespace UsernameVisuals
{
    /// <summary>
    /// Reroutes <c>LinkFlowContainer.AddUserLink()</c> through a gradient text while keeping the
    /// clickable profile-link behaviour.
    /// </summary>
    public sealed class LinkFlowContainerPatch : PluginPatch<UsernameVisualsPlugin>
    {
        private static readonly Lazy<MethodInfo?> applyDefaultParametersMethod = new(() =>
            typeof(TextFlowContainer).GetMethod("ApplyDefaultCreationParameters", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

        public LinkFlowContainerPatch(UsernameVisualsPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, "osu.Game.Graphics.Containers.LinkFlowContainer", "AddUserLink", MethodType.Prefix)
        {
        }

        public static bool Prefix(LinkFlowContainer __instance, IUser user, Action<SpriteText>? creationParameters)
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
    }
}
