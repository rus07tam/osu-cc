using osu.Framework.Localisation;
using osucc.Localisation;
using System.Globalization;

namespace Oii
{
    public static class OiiStrings
    {
        private const string prefix = "oii";

        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString Name => OsuCcLocalisation.Get($"{prefix}:name", "oii");

        public static LocalisableString Description => OsuCcLocalisation.Get($"{prefix}:description", "Shows the improvement indicator next to total play time on user profiles.");

        public static LocalisableString IndicatorTitle => OsuCcLocalisation.Get(getKey(nameof(IndicatorTitle)), "ii");

        public static LocalisableString IndicatorTooltip(double? ii, double? expected, double? pp, double playtimeHours)
            => OsuCcLocalisation.Get(getKey(nameof(IndicatorTooltip)), "ii {0} — {1} hours expected for {2} pp over {3} hours played",
                formatNumber(ii, "0.00"), formatNumber(expected, "0"), formatNumber(pp, "0"), playtimeHours.ToString("0.0", CultureInfo.CurrentCulture));

        private static string formatNumber(double? value, string format)
            => value?.ToString(format, CultureInfo.CurrentCulture) ?? string.Empty;
    }
}
