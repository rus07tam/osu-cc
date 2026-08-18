using osu.Framework.Localisation;
using System.Reflection;

namespace osucc.Localisation
{
    /// <summary>
    /// A <see cref="TranslatableString"/> resolved from the osu!cc JSON registry instead of the
    /// game's localisation stores, using the currently active osu! culture.
    /// </summary>
    public class OsuCcTranslatableString : TranslatableString
    {
        // LocalisableString keeps its inner data in a private field; read it reflectively so a
        // nested LocalisableString argument (e.g. a plugin name inside a template) resolves with
        // the same parameters instead of falling back to its untranslated ToString().
        private static readonly FieldInfo? localisableStringDataField =
            typeof(LocalisableString).GetField("Data", BindingFlags.Instance | BindingFlags.NonPublic);

        public OsuCcTranslatableString(string key, string fallback, params object?[] args)
            : base(key, fallback, args)
        {
        }

        protected override string FormatString(string fallback, object?[] args, LocalisationParameters parameters)
            => base.FormatString(OsuCcLocalisation.Resolve(Key, parameters.Store?.EffectiveCulture?.Name) ?? fallback, resolveArgs(args, parameters), parameters);

        private static object?[] resolveArgs(object?[] args, LocalisationParameters parameters)
        {
            if (args.Length == 0)
                return args;

            object?[] resolved = new object?[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                object? arg = args[i];

                if (arg is LocalisableString localisable)
                {
                    var data = localisableStringDataField?.GetValue(localisable) as ILocalisableStringData;
                    resolved[i] = data?.GetLocalised(parameters) ?? arg;
                }
                else
                {
                    resolved[i] = arg;
                }
            }

            return resolved;
        }
    }
}
