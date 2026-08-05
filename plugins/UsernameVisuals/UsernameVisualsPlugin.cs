using osu.Framework.Graphics.Sprites;
using osucc.Plugin;
using System;
using System.Collections.Generic;

namespace UsernameVisuals
{
    /// <summary>
    /// Username visuals plugin: renders every username across the client with a horizontal
    /// gradient palette, resolved per-user through a public, prioritized conditional API (own
    /// username, fallback for everyone else, per-user overrides). The palette is configurable in
    /// the Specials settings section; the gradient always wins over role/tint colours so the
    /// effect is consistent everywhere. The own username can additionally be replaced with a
    /// custom text or hidden behind a solid block.
    /// </summary>
    public class UsernameVisualsPlugin : IOsuCcPlugin, IOsuCcIconProvider
    {
        private IOsuCcPluginHost host = null!;
        private readonly List<IDisposable?> patches = new();
        private UsernameVisualsApi? api;

        /// <summary>The paint-drip icon, matching the gradient theme.</summary>
        public IconUsage? Icon => FontAwesome.Solid.FillDrip;

        public void Load(IOsuCcPluginHost host)
        {
            this.host = host;

            var settings = host.GetSettings();

            api = new UsernameVisualsApi();
            UsernameVisualsApi.Instance = api;
            api.Attach(settings);

            host.ExportApi(api);
            host.Log("exported public api");

            host.AddSettingsSubsection(() => new UsernameVisualsSettingsSubsection(settings, api));

            installPatches();
            host.Log("loaded");
        }

        public void AttachToGame()
        {
        }

        public void Dispose()
        {
            // Revoke the resolver before unpatching so every already-swapped text re-applies to a
            // plain rendering (the instance is gone) instead of keeping its gradient/override.
            // Text components re-bind on the next frame via their Update() pass.
            if (api != null)
                UsernameVisualsApi.Instance = null;

            foreach (var patch in patches)
                patch?.Dispose();
            patches.Clear();
            GC.SuppressFinalize(this);
        }

        private void installPatches()
        {
            int count = 0;
            if (UserPanelPatch.Install(host) is { } userPanel) { patches.Add(userPanel); count++; }
            if (TopHeaderContainerPatch.Install(host) is { } header) { patches.Add(header); count++; }
            if (LinkFlowContainerPatch.Install(host) is { } links) { patches.Add(links); count++; }
            if (ClickableUsernamePatch.Install(host) is { } clickable) { patches.Add(clickable); count++; }
            if (BeatmapLeaderboardScorePatch.Install(host) is { } beatmap) { patches.Add(beatmap); count++; }
            if (DrawableGameplayLeaderboardScorePatch.Install(host) is { } gameplay) { patches.Add(gameplay); count++; }
            if (DrawableChatUsernamePatch.Install(host) is { } chat) { patches.Add(chat); count++; }
            if (ToolbarUserButtonPatch.Install(host) is { } toolbar) { patches.Add(toolbar); count++; }
            if (ParticipantPanelPatch.Install(host) is { } participants) { patches.Add(participants); count++; }
            if (SpriteTextSetTextPatch.Install(host) is { } setText) { patches.Add(setText); count++; }

            host.Log($"patched {count}/10 username hooks");
        }
    }
}
