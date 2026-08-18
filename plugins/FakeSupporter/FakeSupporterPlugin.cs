using osu.Framework.Graphics.Sprites;
using osucc.Plugin;
using System;
using System.Collections.Generic;

namespace FakeSupporter
{
    /// <summary>
    /// Fake Supporter plugin: visually fakes the current player's osu!supporter tag with a chosen
    /// level everywhere (profile, leaderboards, scores, chat), resolved per-user through a public,
    /// prioritized conditional API plus per-user overrides. Purely cosmetic: nothing is sent to
    /// the servers. The level is configurable in the Specials settings section; per-user overrides
    /// can force any user's supporter state and level.
    /// </summary>
    public class FakeSupporterPlugin : OsuCcPlugin
    {
        private IOsuCcPluginHost host = null!;
        private readonly List<IDisposable?> patches = new();

        /// <summary>The heart icon, matching the supporter theme.</summary>
        public override IconUsage? Icon => FontAwesome.Solid.Heart;

        protected override void OnLoad()
        {

            var settings = host.GetSettings();

            var api = new SupporterFakerApi();
            SupporterFakerApi.Instance = api;
            api.Attach(settings);

            host.ExportApi(api);
            host.Log("exported public api");

            host.AddSettingsSubsection(() => new SupporterFakerSettingsSubsection(settings, api));

            installPatches();
            host.Log("loaded");
        }

        public override void AttachToGame()
        {
        }

        public override void Dispose()
        {
            foreach (var patch in patches)
                patch?.Dispose();
            patches.Clear();
            GC.SuppressFinalize(this);
            base.Dispose();
        }

        private void installPatches()
        {
            int count = 0;
            if (APIRequestPerformPatch.Install(host) is { } perform) { patches.Add(perform); count++; }
            if (LocalUserStateSetLocalUserPatch.Install(host) is { } setLocal) { patches.Add(setLocal); count++; }
            if (LocalUserStateClearLocalUserPatch.Install(host) is { } clearLocal) { patches.Add(clearLocal); count++; }
            if (ToolbarUserButtonLoadPatch.Install(host) is { } toolbar) { patches.Add(toolbar); count++; }
            if (SupporterIconSupportLevelPatch.Install(host) is { } clamp) { patches.Add(clamp); count++; }
            if (UserPanelLoadPatch.Install(host) is { } panel) { patches.Add(panel); count++; }

            host.Log($"patched {count}/6 supporter hooks");
        }
    }
}
