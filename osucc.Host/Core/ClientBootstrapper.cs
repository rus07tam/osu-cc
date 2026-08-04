using osucc.Client;
using osucc.Patches;

namespace osucc.Core
{
    /// <summary>
    /// Installs every Harmony patch once osu.Game.dll is loaded, recording per-patch
    /// success/failure into <see cref="ClientState"/> (which drives the startup toast).
    /// </summary>
    public static class ClientBootstrapper
    {
        public static void InstallPatches()
        {
            var patches = new (string Name, Func<bool> Install)[]
            {
                ("OsuGameBaseCtor", OsuGameBaseCtorPatch.Install),
                ("OsuGameBaseLoad(Sentry)", OsuGameBaseLoadPrefixPatch.Install),
                ("OsuGameBaseLoad", OsuGameBaseLoadPatch.Install),
                ("SettingsOverlay.CreateSections", SettingsOverlayCreateSectionsPatch.Install),
                ("UserModSelectOverlay.ComputeNewMods", UserModComputeNewModsPatch.Install),
                ("ModUtils.CheckValidForGameplay", ModUtilsGameplayPatch.Install),
                ("ModSelectOverlay.createColumns", ModSelectCreateColumnsPatch.Install),
                ("ModSelectOverlay.filterMods", ModSelectFilterModsPatch.Install),
                ("ModSelectOverlay.LoadComplete", ModSelectLoadCompletePatch.Install),
                ("ModSelectFooterContent.CreateButtons", ModSelectFooterCreateButtonsPatch.Install),
                ("Player.ImportScore", PlayerImportScorePatch.Install),
                ("SoloPlayer.CreateTokenRequest", SoloScoreSubmissionPatch.Install),
                ("Toolbar.load", ToolbarLoadPatch.Install),
                ("APIRequest.Perform", APIRequestPerformPatch.Install),
                ("LocalUserState.SetLocalUser", LocalUserStateSetLocalUserPatch.Install),
                ("LocalUserState.ClearLocalUser", LocalUserStateClearLocalUserPatch.Install),
                ("SupporterIcon.set_SupportLevel", SupporterIconSupportLevelPatch.Install),
                ("ToolbarUserButton.load", ToolbarUserButtonLoadPatch.Install),
                ("Panel.PrepareForUse", PanelPrepareForUsePatch.Install),
                ("PaginatedBeatmapContainer.load", PaginatedBeatmapContainerLoadPatch.Install),
            };

            foreach (var (name, install) in patches)
            {
                bool ok;
                try
                {
                    ok = install();
                }
                catch (Exception ex)
                {
                    ok = false;
                    TimingLog.Error($"Bootstrap '{name}': {ex}");
                }

                ClientState.RecordPatchResult(name, ok);
                TimingLog.Info($"Patch '{name}': {(ok ? "OK" : "FAILED")}");
            }
        }
    }
}
