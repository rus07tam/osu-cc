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
        private IOsuCcPluginHost host = null!;
        private readonly List<IDisposable?> patches = new();
        private readonly List<IDisposable?> visualHandles = new();
        private CustomUserGroupsApi? api;
        private IUsernameVisualsApi? visualsApi;

        /// <summary>The user-group icon, matching the theme.</summary>
        public override IconUsage? Icon => FontAwesome.Solid.Users;

        protected override void OnLoad()
        {

            var settings = host.GetSettings();

            api = new CustomUserGroupsApi();
            CustomUserGroupsApi.Instance = api;
            api.Attach(settings);

            host.ExportApi(api);
            host.Log("exported public api");

            host.AddSettingsSubsection(() => new CustomUserGroupsSettingsSubsection(settings, api, host));

            installPatches();
            host.Log("loaded");
        }

        public override void AttachToGame()
        {
            if (api == null)
                return;

            // Optional link: when Username Visuals is loaded, register one low-priority colour rule
            // per distinct group colour so group-coloured usernames also reach the surfaces only it
            // covers (leaderboards, gameplay, …). Priority -1 keeps the user's own-username palette
            // (0) winning and ties against the "others" fallback (also -1) resolve to our later rule.
            visualsApi = host.GetApi<IUsernameVisualsApi>("username-visuals");

            if (visualsApi != null)
            {
                api.Changed += syncVisualRules;
                syncVisualRules();
                host.Log("linked username-visuals colour rules");
            }
        }

        public override void Dispose()
        {
            if (api != null)
                api.Changed -= syncVisualRules;

            foreach (var handle in visualHandles)
                handle?.Dispose();
            visualHandles.Clear();

            foreach (var patch in patches)
                patch?.Dispose();
            patches.Clear();
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

        private void installPatches()
        {
            int count = 0;
            if (APIRequestPerformPatch.Install(host) is { } perform) { patches.Add(perform); count++; }
            if (LocalUserStateSetLocalUserPatch.Install(host) is { } setLocal) { patches.Add(setLocal); count++; }
            if (LocalUserStateClearLocalUserPatch.Install(host) is { } clearLocal) { patches.Add(clearLocal); count++; }

            host.Log($"patched {count}/3 user-group hooks");
        }
    }
}
