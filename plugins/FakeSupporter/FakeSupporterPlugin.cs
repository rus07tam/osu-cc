using osu.Framework.Graphics.Sprites;
using osucc.Plugin;
using System;

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
        /// <summary>The heart icon, matching the supporter theme.</summary>
        public override IconUsage? Icon => FontAwesome.Solid.Heart;

        protected override void OnLoad()
        {

            var settings = Host.GetSettings();

            var api = new SupporterFakerApi();
            SupporterFakerApi.Instance = api;
            api.Attach(settings, Host);

            Host.ExportApi(api);
            Host.Log("exported public api");

            Host.AddSettingsSubsection(() => new SupporterFakerSettingsSubsection(settings, api, Host));

            int count = InstallPatches();
            Host.Log($"patched {count}/6 supporter hooks");
            Host.Log("loaded");
        }

        public override void AttachToGame()
        {
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            base.Dispose();
        }
    }
}
