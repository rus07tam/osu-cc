using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Overlays.Profile;
using osu.Game.Overlays.Profile.Header.Components;
using osucc.Core;

namespace Oii
{
    /// <summary>
    /// Displays the improvement indicator (ii) next to total play time on the profile header: the ratio
    /// of playtime the average player needs for the user's pp against their actual playtime.
    /// </summary>
    public partial class OiiIndicator : CompositeDrawable
    {
        /// <summary>The profile data the indicator is computed from (bound to the play time's source).</summary>
        public Bindable<UserProfileData?> User { get; } = new Bindable<UserProfileData?>();

        private ProfileValueDisplay display = null!;

        public OiiIndicator()
        {
            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = display = new ProfileValueDisplay(minimumWidth: 70)
            {
                Title = OiiStrings.IndicatorTitle,
            };

            User.BindValueChanged(update, true);
        }

        private void update(ValueChangedEvent<UserProfileData?> user)
        {
            var data = user.NewValue;
            var stats = data?.User.Statistics;

            double? pp = stats == null ? null : (double?)stats.PP;
            double? expected = pp is > 0 ? expectedPlaytime(pp.Value, data?.Ruleset.ShortName ?? string.Empty) : null;
            double playtimeHours = (stats?.PlayTime ?? 0) / 3600.0;
            double? ii = expected is > 0 && playtimeHours > 0 ? expected.Value / playtimeHours : null;

            if (ii == null)
            {
                display.Content.Text = "-";
                display.Content.TooltipText = default;
                display.Content.Colour = OsuCcColours.Disabled;
                return;
            }

            display.Content.Text = $"{ii:0.00}x";
            display.Content.TooltipText = OiiStrings.IndicatorTooltip(ii, expected, pp, playtimeHours);
            display.Content.Colour = ii >= 1 ? OsuCcColours.Success : OsuCcColours.Error;
        }

        private static double expectedPlaytime(double pp, string mode) => mode switch
        {
            "osu" => -12 + 0.0781 * pp + 6.01e-6 * pp * pp,
            "taiko" => -1.08 + 0.0179 * pp + 1.65e-6 * pp * pp,
            "mania" => -0.601 + 0.0321 * pp + 7.69e-7 * pp * pp,
            "fruits" => -4.14 + 0.0458 * pp + 2.38e-6 * pp * pp,
            _ => double.NaN,
        };
    }
}
