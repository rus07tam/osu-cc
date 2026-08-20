using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osucc.Client;
using osucc.Plugin;
using osucc.UI.Overlays;
using osuTK;
using System.Linq;

namespace osuccDebug
{
    /// <summary>
    /// Full-screen test overlay in the sheared style (<see cref="OsuCcShearedOverlay"/>): a sheared
    /// header with title/description/close, dimmed background, and a scrollable content area.
    /// Closed via the header close button, back key or clicking outside.
    /// </summary>
    public partial class DebugShearedOverlay : OsuCcShearedOverlay
    {
        private readonly IOsuCcPluginHost host;

        public Bindable<OverlayColourScheme> ColourScheme { get; } =
            new Bindable<OverlayColourScheme>(OverlayColourScheme.Purple);

        public Bindable<string> CustomTitle { get; } =
            new Bindable<string>(string.Empty);

        private readonly FillFlowContainer contentFlow;

        public DebugShearedOverlay(IOsuCcPluginHost host)
            : base(OverlayColourScheme.Purple)
        {
            this.host = host;

            contentFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Padding = new MarginPadding
                {
                    Horizontal = Padding * 2,
                    Vertical = Padding,
                },
            };

            ColourScheme.BindValueChanged(e =>
            {
                ChangeColourScheme(e.NewValue);
                if (IsLoaded)
                    rebuildContent();
            });

            CustomTitle.BindValueChanged(e =>
            {
                if (base.Header is ShearedOverlayHeader header)
                {
                    header.Title = string.IsNullOrWhiteSpace(e.NewValue)
                        ? osuccDebugStrings.ShearedOverlayTitle
                        : (LocalisableString)e.NewValue;
                }
            });
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Header.Title = string.IsNullOrWhiteSpace(CustomTitle.Value)
                ? osuccDebugStrings.ShearedOverlayTitle
                : (LocalisableString)CustomTitle.Value;
            Header.Description = osuccDebugStrings.ShearedOverlayDescription;
            Header.HeaderIcon = OsuIcon.Online;

            MainAreaContent.Add(new OverlayScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = contentFlow,
            });

            rebuildContent();
        }

        private void rebuildContent()
        {
            contentFlow.Clear();

            contentFlow.AddRange(new Drawable[]
            {
                new OsuSpriteText
                {
                    Text = osuccDebugStrings.ShearedOverlayTitle,
                    Font = OsuFont.Torus.With(size: 18, weight: FontWeight.Bold),
                },
                new OsuSpriteText
                {
                    Text = osuccDebugStrings.ShearedOverlayBodyText,
                    Font = OsuFont.Default.With(size: 14),
                },
                new SettingsTextBox
                {
                    LabelText = osuccDebugStrings.CustomTitleLabel,
                    Current = CustomTitle,
                    Margin = new MarginPadding { Top = 6 },
                },
                new SettingsEnumDropdown<OverlayColourScheme>
                {
                    LabelText = osuccDebugStrings.WaveOverlayColourSchemeLabel,
                    Current = ColourScheme,
                    Margin = new MarginPadding { Top = 6 },
                },
                new SettingsButtonV2
                {
                    Text = osuccDebugStrings.ShearedOverlayNotifyButton,
                    Margin = new MarginPadding { Top = 6 },
                    Action = () => host.Notify(osuccDebugStrings.ShearedOverlayNotified, NotificationKind.Info),
                },
                new OsuSpriteText
                {
                    Text = osuccDebugStrings.WaveOverlayWaveBandsLabel,
                    Font = OsuFont.Default.With(size: 13),
                    Margin = new MarginPadding { Top = 10 },
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(6),
                    Children = createSwatches(),
                },
            });
        }

        private Drawable[] createSwatches()
        {
            var bands = new (LocalisableString Title, Colour4 Colour)[]
            {
                (osuccDebugStrings.WaveBandLight4, ColourProvider.Light4),
                (osuccDebugStrings.WaveBandLight3, ColourProvider.Light3),
                (osuccDebugStrings.WaveBandDark4, ColourProvider.Dark4),
                (osuccDebugStrings.WaveBandDark3, ColourProvider.Dark3),
            };

            return bands.Select(band => new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Children = new Drawable[]
                {
                    new Box
                    {
                        Size = new Vector2(120, 26),
                        Colour = band.Colour,
                    },
                    new OsuSpriteText
                    {
                        Text = band.Title,
                        Font = OsuFont.Default.With(size: 11),
                    },
                },
            }).ToArray();
        }
    }
}
