using osucc.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace osucc.Core
{
    /// <summary>
    /// Discovers classes decorated with <see cref="OsuCcPatchAttribute"/> /
    /// <see cref="OsuCcConstructorPatchAttribute"/> inside a plugin assembly and installs them
    /// through the scoped host — no manual <c>Install</c> methods or handle bookkeeping. Plugins
    /// trigger this via <see cref="OsuCcPlugin.InstallPatches"/>.
    /// </summary>
    public static class OsuCcPatches
    {
        /// <summary>
        /// Scans <paramref name="assembly"/> for declarative patch classes and applies them via
        /// <paramref name="host"/>. Before installing, the scoped host is assigned to a static
        /// settable property/field of type <see cref="IOsuCcPluginHost"/> on each patch class
        /// (named <c>Host</c>/<c>host</c> when present), so patch code can reach the host typed
        /// without casts. Returns the number of targets that were actually patched; failures are
        /// logged at <see cref="LogLevel.Warn"/> and skipped.
        /// </summary>
        public static int Install(IOsuCcPluginHost host, Assembly assembly)
        {
            int count = 0;

            foreach (var type in GetLoadableTypes(assembly))
            {
                if (!HasPatchAttributes(type))
                    continue;

                InjectHost(type, host);

                foreach (var attribute in type.GetCustomAttributes<OsuCcPatchAttribute>(false))
                {
                    if (Apply(host, type, new Target(attribute)))
                        count++;
                }

                foreach (var attribute in type.GetCustomAttributes<OsuCcConstructorPatchAttribute>(false))
                {
                    if (Apply(host, type, new Target(attribute)))
                        count++;
                }
            }

            return count;
        }

        private static bool Apply(IOsuCcPluginHost host, Type patchType, Target target)
        {
            var methodBase = target.Resolve();
            if (methodBase == null)
            {
                host.Log(LogLevel.Warn, $"patch target {target.Describe} not found ({patchType.Name})");
                return false;
            }

            var patchMethodName = target.Kind.ToString();
            if (patchType.GetMethod(patchMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) == null)
            {
                host.Log(LogLevel.Warn, $"patch {patchType.Name}: no '{patchMethodName}' method");
                return false;
            }

            if (host.AddPatch(methodBase, patchType, patchMethodName, target.Kind) == null)
            {
                host.Log(LogLevel.Warn, $"patch target {target.Describe} unavailable ({patchType.Name})");
                return false;
            }

            return true;
        }

        private static void InjectHost(Type type, IOsuCcPluginHost host)
        {
            var candidates = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                 .Where(p => p.SetMethod != null && p.PropertyType.IsInstanceOfType(host))
                                 .Cast<MemberInfo>()
                                 .Concat(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                             .Where(f => !f.IsInitOnly && f.FieldType.IsInstanceOfType(host)));

            var member = candidates.FirstOrDefault(m => m.Name == "Host" || m.Name == "host")
                         ?? candidates.FirstOrDefault();

            switch (member)
            {
                case PropertyInfo property:
                    property.SetValue(null, host, null);
                    break;

                case FieldInfo field:
                    field.SetValue(null, host);
                    break;
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(t => t != null)!;
            }
        }

        private static bool HasPatchAttributes(Type type)
            => type.GetCustomAttributes<OsuCcPatchAttribute>(false).Any()
               || type.GetCustomAttributes<OsuCcConstructorPatchAttribute>(false).Any();

        private readonly struct Target
        {
            public Target(OsuCcPatchAttribute attribute)
            {
                typeName = attribute.TypeName;
                type = attribute.Type;
                methodName = attribute.MethodName;
                constructorParameters = null;
                Kind = attribute.PatchType;
            }

            public Target(OsuCcConstructorPatchAttribute attribute)
            {
                typeName = attribute.TypeName;
                type = null;
                methodName = null;
                constructorParameters = attribute.ParameterTypes;
                Kind = MethodType.Postfix;
            }

            private readonly string? typeName;
            private readonly Type? type;
            private readonly string? methodName;
            private readonly Type[]? constructorParameters;

            public MethodType Kind { get; }

            public MethodBase? Resolve()
            {
                if (type != null)
                    return type.GetMethod(methodName!, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (methodName != null)
                    return Reflection.GetMethod(typeName!, methodName);

                return Reflection.GetConstructor(typeName!, constructorParameters!);
            }

            public string Describe
                => type != null ? $"{type.FullName}.{methodName}"
                   : methodName != null ? $"{typeName}.{methodName}"
                   : $"{typeName} constructor";
        }
    }
}
