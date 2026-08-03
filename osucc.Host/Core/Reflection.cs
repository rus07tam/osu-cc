using HarmonyLib;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osu.Game;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using System.Collections.Concurrent;
using System.Reflection;

namespace osucc.Core
{
    /// <summary>
    /// Central place for the small bits of reflection the client still needs.
    /// Everything here resolves against the *runtime* assembly identities, so it stays
    /// correct regardless of the exact production build the hook is loaded into.
    /// </summary>
    public static class Reflection
    {
        /// <summary>The loaded <c>osu.Game</c> assembly, or <c>null</c> before it is loaded.</summary>
        public static Assembly? GetGameAssembly()
            => AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == "osu.Game");

        /// <summary>A type from the loaded <c>osu.Game</c> assembly, or <c>null</c>.</summary>
        public static Type? GetGameType(string fullName)
            => GetGameAssembly()?.GetType(fullName);

        /// <summary>A field resolved by name against a runtime type, cached per (type, name).</summary>
        public static FieldInfo? GetField(string typeName, string fieldName)
        {
            var key = (typeName, fieldName);

            if (fields.TryGetValue(key, out var cached))
                return cached;

            var field = GetGameType(typeName)?.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            fields[key] = field;
            return field;
        }

        /// <summary>A method resolved by name against a runtime type, cached per (type, name). Non-throwing on ambiguity (first match wins).</summary>
        public static MethodInfo? GetMethod(string typeName, string methodName)
            => GetMethod(typeName, methodName, null);

        /// <summary>
        /// Resolves a method by name against a runtime type, disambiguating overloads with
        /// <paramref name="predicate"/>. Never cached (a delegate cannot key the cache), so use it
        /// only at install time, not on hot paths.
        /// </summary>
        public static MethodInfo? GetMethod(string typeName, string methodName, Func<MethodInfo, bool>? predicate)
        {
            var methods = GetGameType(typeName)?.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                               .Where(m => m.Name == methodName);

            if (predicate != null)
                methods = methods?.Where(predicate);

            return methods?.FirstOrDefault();
        }

        /// <summary>A constructor resolved by signature against a runtime type, cached per (type, signature).</summary>
        public static ConstructorInfo? GetConstructor(string typeName, params Type[] parameterTypes)
        {
            var signature = string.Join(",", parameterTypes.Select(t => t.FullName));
            var key = (typeName, signature);

            if (constructors.TryGetValue(key, out var cached))
                return cached;

            var constructor = GetGameType(typeName)?.GetConstructor(parameterTypes);
            constructors[key] = constructor;
            return constructor;
        }

        /// <summary>Reads a public property-or-field (e.g. a bindable's <c>Value</c>) off a weakly-typed instance.</summary>
        public static object? GetPropertyOrField(object instance, string name)
        {
            var type = instance.GetType();
            var member = (MemberInfo?)type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
                ?? type.GetField(name, BindingFlags.Instance | BindingFlags.Public);

            return member switch
            {
                FieldInfo field => field.GetValue(instance),
                PropertyInfo property => property.GetValue(instance),
                _ => null,
            };
        }

        /// <summary>Writes a public property-or-field off a weakly-typed instance. No-op when the member does not exist.</summary>
        public static void SetPropertyOrField(object instance, string name, object? value)
        {
            var type = instance.GetType();
            var member = (MemberInfo?)type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
                ?? type.GetField(name, BindingFlags.Instance | BindingFlags.Public);

            switch (member)
            {
                case FieldInfo field:
                    field.SetValue(instance, value);
                    break;

                case PropertyInfo property:
                    property.SetValue(instance, value);
                    break;
            }
        }

        private static readonly ConcurrentDictionary<(string, string), FieldInfo?> fields = new();
        private static readonly ConcurrentDictionary<(string, string), ConstructorInfo?> constructors = new();

        /// <summary>A <see cref="HarmonyMethod"/> wrapping a private static method of the given patch type.</summary>
        public static HarmonyMethod HarmonyMethod(Type patchType, string name)
            => new(patchType.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!);

        public static string? GetName(Drawable? drawable)
            => drawable == null ? null : nameField(drawable)?.GetValue(drawable) as string;

        public static void SetName(Drawable? drawable, string value)
        {
            if (drawable != null)
                nameField(drawable)?.SetValue(drawable, value);
        }

        private static FieldInfo? nameField(Drawable drawable)
        {
            var type = drawable.GetType();

            if (nameFields.TryGetValue(type, out var cached))
                return cached;

            var field = type.GetField("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            nameFields[type] = field;
            return field;
        }

        private static readonly Dictionary<Type, FieldInfo?> nameFields = new();

        public static Storage? GetStorage(osu.Framework.Game? game)
        {
            if (game == null)
                return null;

            // Storage is a protected property declared on osu.Game.OsuGameBase
            // (not on osu.Framework.Game), resolved against the live runtime type.
            var prop = game.GetType().GetProperty("Storage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            if (prop?.GetValue(game) is Storage storage)
                return storage;

            // Fallback: OsuGameBase caches Storage in its dependency container.
            return game.Dependencies?.Get(typeof(Storage)) as Storage;
        }

        public static INotificationOverlay? GetNotificationOverlay(osu.Framework.Game? game)
            => game == null ? null
                : game.GetType().GetField("Notifications", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)?.GetValue(game) as INotificationOverlay;

        /// <summary>
        /// The game's update-thread <see cref="Scheduler"/>. <c>Drawable.Scheduler</c> is
        /// protected, so it is read reflectively from the runtime game instance.
        /// </summary>
        public static Scheduler? GetScheduler(osu.Framework.Game? game)
        {
            if (game == null)
                return null;

            var prop = game.GetType().GetProperty("Scheduler", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            return prop?.GetValue(game) as Scheduler;
        }

        /// <summary>The update-thread <see cref="Scheduler"/> of a drawable, read reflectively (the property is protected).</summary>
        public static Scheduler? GetScheduler(Drawable drawable)
        {
            if (drawable == null)
                return null;

            try
            {
                var property = typeof(Drawable).GetProperty("Scheduler", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                return property?.GetValue(drawable) as Scheduler;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// The game's top-most overlay container (the one the game's own <c>MedalOverlay</c>
        /// and other full-screen pop-ups are added to). Declared as a private field on
        /// <c>osu.Game.OsuGame</c>, so the hierarchy is walked to find the declaring type.
        /// </summary>
        public static osu.Framework.Graphics.Containers.Container? GetTopMostOverlayContent(osu.Framework.Game? game)
        {
            if (game == null)
                return null;

            var field = FindField(game.GetType(), "topMostOverlayContent");
            return field?.GetValue(game) as osu.Framework.Graphics.Containers.Container;
        }

        public static osu.Game.Overlays.DialogOverlay? GetDialogOverlay(osu.Framework.Game? game)
        {
            if (game == null)
                return null;

            // dialogOverlay is a *private* field declared on osu.Game.OsuGame. GetField()
            // on the runtime type (OsuGameDesktop) cannot see private members of a base
            // class, so walk the hierarchy to find the declaring type and read from it.
            var field = FindField(game.GetType(), "dialogOverlay");
            return field?.GetValue(game) as osu.Game.Overlays.DialogOverlay;
        }

        /// <summary>
        /// Registers a full-screen blocking overlay through the game's
        /// <c>IOverlayManager</c>, which places it in <c>overlayContent</c> — the layer
        /// above the screens. Returns the registration token, or <c>null</c> if the game
        /// instance does not implement the (internal) interface.
        /// </summary>
        /// <remarks>
        /// The interface is internal to osu.Game, so it is resolved and invoked via
        /// reflection against the live <see cref="osu.Game.OsuGameBase"/> assembly. The
        /// overlay must not have a parent (the manager asserts <c>Parent == null</c>).
        /// </remarks>
        public static IDisposable? RegisterBlockingOverlay(osu.Framework.Game? game, osu.Framework.Graphics.Containers.OverlayContainer overlay)
        {
            if (game == null)
                return null;

            var overlayManagerType = typeof(OsuGameBase).Assembly.GetType("osu.Game.Overlays.IOverlayManager");
            if (overlayManagerType == null || !overlayManagerType.IsInstanceOfType(game))
                return null;

            var method = overlayManagerType.GetMethod("RegisterBlockingOverlay", new[] { typeof(osu.Framework.Graphics.Containers.OverlayContainer) });
            return method?.Invoke(game, new object[] { overlay }) as IDisposable;
        }

        /// <summary>
        /// Finds a field across the full type hierarchy (including private base members),
        /// which <see cref="Type.GetField(string, BindingFlags)"/> with FlattenHierarchy
        /// does not do for instance fields.
        /// </summary>
        public static FieldInfo? FindField(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
            }

            return null;
        }
    }
}
