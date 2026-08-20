using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osuTK;
using System;

namespace osuccDebug
{
    /// <summary>
    /// Debug panel for the full-screen overlay styles: opens the wave overlay
    /// (<see cref="DebugWaveOverlay"/>) and sheared overlay (<see cref="DebugShearedOverlay"/>).
    /// </summary>
    public partial class OverlayTestPanel : FillFlowContainer
    {
        public OverlayTestPanel(
            Bindable<string> customTitle,
            Bindable<OverlayColourScheme> colourScheme,
            Action showWaveOverlay,
            Action showShearedOverlay)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, 10);

            Children = new Drawable[]
            {
                new SettingsTextBox
                {
                    LabelText = osuccDebugStrings.CustomTitleLabel,
                    Current = customTitle,
                },
                new SettingsEnumDropdown<OverlayColourScheme>
                {
                    LabelText = osuccDebugStrings.WaveOverlayColourSchemeLabel,
                    Current = colourScheme,
                },
                new SettingsButtonV2
                {
                    Text = osuccDebugStrings.ShowWaveOverlayButton,
                    Action = showWaveOverlay,
                },
                new SettingsButtonV2
                {
                    Text = osuccDebugStrings.ShowShearedOverlayButton,
                    Action = showShearedOverlay,
                },
            };
        }
    }
}
