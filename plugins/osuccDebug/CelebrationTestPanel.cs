using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Overlays.Settings;
using osucc.Celebrations;
using osuTK;

namespace osuccDebug
{
    /// <summary>
    /// Debug panel for building a fully customised <see cref="Celebration"/> and showing it via
    /// <see cref="ClientCelebrations"/>. Every <see cref="CelebrationOptions"/> field worth
    /// tweaking is exposed as a settings control.
    /// </summary>
    public partial class CelebrationTestPanel : FillFlowContainer
    {
        private readonly Bindable<string> title = new Bindable<string>("NEW BEST SCORE!");

        private readonly Bindable<string> subtitle = new Bindable<string>("1,234,567");

        private readonly Bindable<Colour4> accent = new Bindable<Colour4>(Color4Extensions.FromHex("66ccff"));

        private readonly BindableFloat backgroundDim = new BindableFloat(0.7f)
        {
            MinValue = 0,
            MaxValue = 1,
        };

        private readonly BindableDouble totalDuration = new BindableDouble(4600)
        {
            MinValue = 1000,
            MaxValue = 20000,
        };

        private readonly BindableDouble particleDuration = new BindableDouble(500)
        {
            MinValue = 100,
            MaxValue = 3000,
        };

        public CelebrationTestPanel()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, 10);

            Children = new Drawable[]
            {
                new SettingsTextBox
                {
                    LabelText = osuccDebugStrings.TitleLabel,
                    Current = title,
                },
                new SettingsTextBox
                {
                    LabelText = osuccDebugStrings.SubtitleLabel,
                    Current = subtitle,
                },
                new SettingsColour
                {
                    LabelText = osuccDebugStrings.AccentColourLabel,
                    Current = accent,
                },
                new SettingsSlider<float>
                {
                    LabelText = osuccDebugStrings.BackgroundDimLabel,
                    Current = backgroundDim,
                    KeyboardStep = 0.01f,
                },
                new SettingsSlider<double>
                {
                    LabelText = osuccDebugStrings.TotalDurationLabel,
                    Current = totalDuration,
                    KeyboardStep = 100,
                },
                new SettingsSlider<double>
                {
                    LabelText = osuccDebugStrings.ParticleDurationLabel,
                    Current = particleDuration,
                    KeyboardStep = 50,
                },
                new SettingsButtonV2
                {
                    Text = osuccDebugStrings.ShowPersonalBestButton,
                    Action = showCelebration,
                },
            };
        }

        private void showCelebration()
        {
            var options = new CelebrationOptions
            {
                TitleText = title.Value,
                SubtitleText = subtitle.Value,
                AccentColour = accent.Value,
                BackgroundDim = backgroundDim.Value,
                TotalDuration = totalDuration.Value,
                ParticleDuration = particleDuration.Value,
            };

            ClientCelebrations.Show(new Celebration(options));
        }
    }
}
