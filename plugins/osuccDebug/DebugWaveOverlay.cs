using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
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
    /// Full-screen test overlay in the wave style (<see cref="OsuCcWaveOverlay"/>): the coloured
    /// bands sweep over the dimmed background while the page fades in — a stock-style header
    /// (icon, title, description, tabs) scrolling together with the main area. Tabs switch between
    /// an overview and a swatch viewer of the four band colours straight from the overlay's
    /// <see cref="OverlayColourProvider"/>. Closed via back / click outside.
    /// </summary>
    public partial class DebugWaveOverlay : OsuCcWaveOverlay
    {
        private readonly IOsuCcPluginHost host;

        public Bindable<OverlayColourScheme> ColourScheme { get; } =
            new Bindable<OverlayColourScheme>(OverlayColourScheme.Blue);

        public Bindable<string> CustomTitle { get; } =
            new Bindable<string>(string.Empty);

        private DebugWaveSection currentSection = DebugWaveSection.Overview;
        private TabControlOverlayHeader<DebugWaveSection>.OverlayHeaderTabControl? tabs;

        public DebugWaveOverlay(IOsuCcPluginHost host)
            : base(OverlayColourScheme.Blue)
        {
            this.host = host;

            ColourScheme.BindValueChanged(e =>
            {
                ChangeColourScheme(e.NewValue);
                if (tabs != null)
                    tabs.AccentColour = ColourProvider.Highlight1;
                if (IsLoaded)
                    showSection(currentSection);
            });

            CustomTitle.BindValueChanged(e =>
            {
                if (base.Header is OsuCcOverlayHeader header)
                {
                    header.TitleText = string.IsNullOrWhiteSpace(e.NewValue)
                        ? osuccDebugStrings.WaveOverlayTitle
                        : (LocalisableString)e.NewValue;
                }
            });
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Header.TitleText = string.IsNullOrWhiteSpace(CustomTitle.Value)
                ? osuccDebugStrings.WaveOverlayTitle
                : (LocalisableString)CustomTitle.Value;
            Header.DescriptionText = osuccDebugStrings.WaveOverlayDescription;
            Header.HeaderIcon = OsuIcon.Online;

            tabs = new TabControlOverlayHeader<DebugWaveSection>.OverlayHeaderTabControl
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
            };
            tabs.Current.BindValueChanged(e =>
            {
                currentSection = e.NewValue;
                showSection(e.NewValue);
            }, true);
            Header.ContentRow.Add(tabs);
        }

        private void showSection(DebugWaveSection section)
        {
            currentSection = section;
            MainAreaContent.Clear();

            switch (section)
            {
                case DebugWaveSection.Overview:
                    MainAreaContent.Add(createOverviewContent());
                    break;

                case DebugWaveSection.Colours:
                    MainAreaContent.Add(createColoursContent());
                    break;

                case DebugWaveSection.Cards:
                    MainAreaContent.Add(new PluginCardsTestPanel(host));
                    break;
            }
        }

        private FillFlowContainer createOverviewContent() => new FillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Vertical,
            Spacing = new Vector2(0, 10),
            Padding = new MarginPadding
            {
                Vertical = Padding,
            },
            Children = new Drawable[]
            {
                new OsuSpriteText
                {
                    Text = osuccDebugStrings.WaveOverlaySectionTitle,
                    Font = OsuFont.Torus.With(size: 18, weight: FontWeight.Bold),
                },
                new OsuSpriteText
                {
                    Text = osuccDebugStrings.WaveOverlayBodyText,
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
                    Text = osuccDebugStrings.WaveOverlayNotifyButton,
                    Margin = new MarginPadding { Top = 6 },
                    Action = () => host.Notify(osuccDebugStrings.WaveOverlayNotified, NotificationKind.Info),
                },
            },
        };

        private FillFlowContainer createColoursContent() => new FillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Vertical,
            Spacing = new Vector2(0, 10),
            Padding = new MarginPadding
            {
                Vertical = Padding,
            },
            Children = new Drawable[]
            {
                new SettingsEnumDropdown<OverlayColourScheme>
                {
                    LabelText = osuccDebugStrings.WaveOverlayColourSchemeLabel,
                    Current = ColourScheme,
                },
                new OsuSpriteText
                {
                    Text = osuccDebugStrings.WaveOverlayWaveBandsLabel,
                    Font = OsuFont.Default.With(size: 13),
                    Margin = new MarginPadding { Top = 6 },
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(6),
                    Children = createWaveSwatches(),
                },
            },
        };

        private Drawable[] createWaveSwatches()
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

        private enum DebugWaveSection
        {
            [LocalisableDescription(typeof(osuccDebugStrings), nameof(osuccDebugStrings.WaveOverlayOverviewTab))]
            Overview,

            [LocalisableDescription(typeof(osuccDebugStrings), nameof(osuccDebugStrings.WaveOverlayColoursTab))]
            Colours,

            [LocalisableDescription(typeof(osuccDebugStrings), nameof(osuccDebugStrings.WaveOverlayCardsTab))]
            Cards,
        }
    }
}
