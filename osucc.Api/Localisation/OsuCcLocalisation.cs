using osu.Framework.Localisation;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace osucc.Localisation
{
    /// <summary>
    /// Culture-keyed string registry, merged from embedded <c>Localisation/&lt;culture&gt;.json</c>
    /// resources of the host and every plugin assembly.
    /// </summary>
    public static class OsuCcLocalisation
    {
        private const string resourceMarker = ".Localisation.";

        private static readonly object lockObject = new();
        private static readonly Dictionary<string, Dictionary<string, string>> byCulture = new();

        static OsuCcLocalisation()
        {
            RegisterAssembly(typeof(OsuCcLocalisation).Assembly);
        }

        public static void RegisterAssembly(Assembly assembly)
        {
            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                string? culture = cultureFromResourceName(resourceName);

                if (culture == null)
                    continue;

                Dictionary<string, string> values;

                try
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);

                    if (stream == null)
                        continue;

                    values = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new Dictionary<string, string>();
                }
                catch
                {
                    continue;
                }

                lock (lockObject)
                {
                    if (!byCulture.TryGetValue(culture, out var map))
                        byCulture[culture] = map = new Dictionary<string, string>();

                    foreach (var (key, value) in values)
                        map[key] = value;
                }
            }
        }

        /// <summary>Looks up a key for the given culture, falling back to parent cultures; <c>null</c> when untranslated.</summary>
        public static string? Resolve(string key, string? culture)
        {
            while (!string.IsNullOrEmpty(culture))
            {
                lock (lockObject)
                {
                    if (byCulture.TryGetValue(culture, out var map) && map.TryGetValue(key, out string? text))
                        return text;
                }

                string? parent;

                try
                {
                    parent = new CultureInfo(culture).Parent.Name;
                }
                catch (CultureNotFoundException)
                {
                    return null;
                }

                if (parent == culture)
                    break;

                culture = parent;
            }

            return null;
        }

        /// <summary>Builds a localisable string resolved from the registry, using <paramref name="fallback"/> when untranslated.</summary>
        public static LocalisableString Get(string key, string fallback, params object?[] args)
            => new LocalisableString(new OsuCcTranslatableString(key, fallback, args));

        private static string? cultureFromResourceName(string resourceName)
        {
            if (!resourceName.EndsWith(".json", StringComparison.Ordinal))
                return null;

            int index = resourceName.LastIndexOf(resourceMarker, StringComparison.Ordinal);

            if (index < 0)
                return null;

            string culture = resourceName[(index + resourceMarker.Length)..^".json".Length];

            if (culture.Length is < 2 or > 9 || !culture.All(c => char.IsLetter(c) || c == '-'))
                return null;

            return culture;
        }
    }
}
