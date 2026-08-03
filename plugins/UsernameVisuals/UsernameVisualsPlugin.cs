using osu.Framework.Graphics.Sprites;
using osucc.Plugin;

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
    [OsuCcPlugin(
        "username-visuals",
        "Username Visuals",
        Author = "osu-cc",
        Description = "Username visuals plus an own-username display override (custom text / hide).",
        Version = "1.0.0")]
    public class UsernameVisualsPlugin : IOsuCcPlugin, IOsuCcIconProvider
    {
        private IOsuCcPluginHost host = null!;

        /// <summary>The paint-drip icon, matching the gradient theme.</summary>
        public IconUsage? Icon => FontAwesome.Solid.FillDrip;

        public void Load(IOsuCcPluginHost host)
        {
            this.host = host;

            var settings = host.GetSettings();

            var api = new UsernameVisualsApi();
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
            GC.SuppressFinalize(this);
        }

        private void installPatches()
        {
            var harmony = host.CreateHarmony("username-visuals");

            int count = 0;
            if (UserPanelPatch.Install(harmony)) count++;
            if (TopHeaderContainerPatch.Install(harmony)) count++;
            if (LinkFlowContainerPatch.Install(harmony)) count++;
            if (ClickableUsernamePatch.Install(harmony)) count++;
            if (BeatmapLeaderboardScorePatch.Install(harmony)) count++;
            if (DrawableGameplayLeaderboardScorePatch.Install(harmony)) count++;
            if (DrawableChatUsernamePatch.Install(harmony)) count++;
            if (ToolbarUserButtonPatch.Install(harmony)) count++;
            if (ParticipantPanelPatch.Install(harmony)) count++;
            if (SpriteTextSetTextPatch.Install(harmony)) count++;

            host.Log($"patched {count}/10 username hooks");
        }
    }
}
