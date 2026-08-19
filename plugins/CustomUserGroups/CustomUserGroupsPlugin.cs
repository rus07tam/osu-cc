using osu.Framework.Graphics.Sprites;
using osucc.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using UsernameVisuals;

namespace CustomUserGroups
{
    /// <summary>
    /// Custom User Groups plugin: shows custom user groups — badges and username colour — on top of
    /// the real ones everywhere groups are displayed (profiles, user cards), with an editable group
    /// library, per-user overrides and a public, prioritized rule API. Purely cosmetic: nothing is
    /// sent to the servers.
    /// </summary>
    public class CustomUserGroupsPlugin : OsuCcPlugin
    {
        private readonly List<IDisposable?> visualHandles = new();
        private CustomUserGroupsApi? api;
        private IUsernameVisualsApi? visualsApi;

        /// <summary>The user-group icon, matching the theme.</summary>
        public override IconUsage? Icon => FontAwesome.Solid.Users;

        protected override void OnLoad()
        {

            var settings = Host.GetSettings();

            api = new CustomUserGroupsApi();
            CustomUserGroupsApi.Instance = api;
            api.Attach(settings, Host);

            Host.ExportApi(api);
            Host.Log("exported public api");

            Host.AddSettingsSubsection(() => new CustomUserGroupsSettingsSubsection(settings, api, Host));

            int count = InstallPatches();
            Host.Log($"patched {count}/3 user-group hooks");
            Host.Log("loaded");
        }

        public override void AttachToGame()
        {
            if (api == null)
                return;

            // Optional link: when Username Visuals is loaded, register one low-priority colour rule
            // per distinct group colour so group-coloured usernames also reach the surfaces only it
            // covers (leaderboards, gameplay, …). Priority -1 keeps the user's own-username palette
            // (0) winning and ties against the "others" fallback (also -1) resolve to our later rule.
            visualsApi = Host.GetApi<IUsernameVisualsApi>("username-visuals");

            if (visualsApi != null)
            {
                api.Changed += syncVisualRules;
                syncVisualRules();
                Host.Log("linked username-visuals colour rules");
            }
        }

        public override void Dispose()
        {
            if (api != null)
                api.Changed -= syncVisualRules;

            foreach (var handle in visualHandles)
                handle?.Dispose();
            visualHandles.Clear();

            GC.SuppressFinalize(this);
            base.Dispose();
        }

        private void syncVisualRules()
        {
            if (visualsApi == null)
                return;

            foreach (var handle in visualHandles)
                handle?.Dispose();
            visualHandles.Clear();

            var colours = api!.Groups
                            .Where(g => g.Colour is { Length: > 0 })
                            .Select(g => g.Colour!)
                            .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (string colour in colours)
            {
                // Single-colour palette parsed through the shared helper to match the framework's Colour4 type.
                var palette = SettingsSubsectionExtensions.ParsePalette(colour);
                if (palette.Length == 0)
                    continue;

                visualHandles.Add(visualsApi.AddColourRule(
                    context => string.Equals(api.ResolveColour(context.User), colour, StringComparison.OrdinalIgnoreCase),
                    palette,
                    priority: -1));
            }
        }
    }
}
