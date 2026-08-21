using osucc.Client;
using osucc.Patches;
using System;

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
            var patches = new (string Name, OsuCcPatch Patch)[]
            {
                ("OsuGameBaseCtor", new OsuGameBaseCtorPatch()),
                ("OsuGameBaseLoad", new OsuGameBaseLoadPatch()),
                ("SettingsOverlay.CreateSections", new SettingsOverlayCreateSectionsPatch()),
                ("UserModSelectOverlay.ComputeNewMods", new UserModComputeNewModsPatch()),
                ("ModUtils.CheckValidForGameplay", new ModUtilsGameplayPatch()),
                ("ModSelectOverlay.createColumns", new ModSelectCreateColumnsPatch()),
                ("ModSelectOverlay.filterMods", new ModSelectFilterModsPatch()),
                ("ModSelectOverlay.LoadComplete", new ModSelectLoadCompletePatch()),
                ("ModSelectFooterContent.CreateButtons", new ModSelectFooterCreateButtonsPatch()),
                ("Player.LoadComplete", new PlayerBreakSkipPatch()),
                ("Player.ImportScore", new PlayerImportScorePatch()),
                ("SoloPlayer.CreateTokenRequest", new SoloScoreSubmissionPatch()),
                ("Toolbar.load", new ToolbarLoadPatch()),
                ("Panel.PrepareForUse", new PanelPrepareForUsePatch()),
                ("PaginatedBeatmapContainer.load", new PaginatedBeatmapContainerLoadPatch()),
                ("OverlayColourProvider.getColour/getAccentColour", new OverlayColourProviderThemePatch()),
                ("InputManager.Handle", new InputManagerHandlePatch()),
            };

            foreach (var (name, patch) in patches)
            {
                bool ok;
                try
                {
                    ok = patch.Install(HookDependencies.Main);
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
