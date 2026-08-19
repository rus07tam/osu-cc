using osu.Framework.Graphics.Sprites;
using osucc.Plugin;
using System;

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

            int count = InstallPatches();
            Host.Log($"patched {count}/10 username hooks");
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

            GC.SuppressFinalize(this);
            base.Dispose();
        }
    }
}
