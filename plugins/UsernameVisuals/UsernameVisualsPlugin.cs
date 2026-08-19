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
    public class UsernameVisualsPlugin : OsuCcPlugin
    {
        private readonly List<IDisposable?> patches = new();
        private UsernameVisualsApi? api;

        /// <summary>The paint-drip icon, matching the gradient theme.</summary>
        public override IconUsage? Icon => FontAwesome.Solid.FillDrip;

        protected override void OnLoad()
        {

            var settings = Host.GetSettings();

            api = new UsernameVisualsApi();
            UsernameVisualsApi.Instance = api;
            api.Attach(settings);

            Host.ExportApi(api);
            Host.Log("exported public api");

            Host.AddSettingsSubsection(() => new UsernameVisualsSettingsSubsection(settings, api, Host));

            installPatches();
            Host.Log("loaded");
        }

        public override void AttachToGame()
        {
        }

        public override void Dispose()
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
            base.Dispose();
        }

        private void installPatches()
        {
            int count = 0;
            if (UserPanelPatch.Install(Host) is { } userPanel) { patches.Add(userPanel); count++; }
            if (TopHeaderContainerPatch.Install(Host) is { } header) { patches.Add(header); count++; }
            if (LinkFlowContainerPatch.Install(Host) is { } links) { patches.Add(links); count++; }
            if (ClickableUsernamePatch.Install(Host) is { } clickable) { patches.Add(clickable); count++; }
            if (BeatmapLeaderboardScorePatch.Install(Host) is { } beatmap) { patches.Add(beatmap); count++; }
            if (DrawableGameplayLeaderboardScorePatch.Install(Host) is { } gameplay) { patches.Add(gameplay); count++; }
            if (DrawableChatUsernamePatch.Install(Host) is { } chat) { patches.Add(chat); count++; }
            if (ToolbarUserButtonPatch.Install(Host) is { } toolbar) { patches.Add(toolbar); count++; }
            if (ParticipantPanelPatch.Install(Host) is { } participants) { patches.Add(participants); count++; }
            if (SpriteTextSetTextPatch.Install(Host) is { } setText) { patches.Add(setText); count++; }

            Host.Log($"patched {count}/10 username hooks");
        }
    }
}
