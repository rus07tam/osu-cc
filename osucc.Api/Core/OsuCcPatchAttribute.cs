using System;

namespace osucc.Core
{
    /// <summary>
    /// Declaratively marks a class as a Harmony patch for a method of the osu! game. The
    /// attribute is placed on the *class*; the patch method is inferred by name from
    /// <see cref="PatchType"/> (<c>Postfix</c>/<c>Prefix</c>/<c>Transpiler</c>). Multiple
    /// attributes can decorate a single class to patch several targets. The class needs no
    /// manual <c>Install</c> method — <see cref="OsuCcPatches.Install"/> discovers and applies it.
    /// A static settable property/field of type <see cref="osucc.Plugin.IOsuCcPluginHost"/> is
    /// assigned the scoped host before installation, so patch code can reach the host typed
    /// without casts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class OsuCcPatchAttribute : Attribute
    {
        /// <summary>Targets a method of a runtime osu.Game type resolved by name.</summary>
        public OsuCcPatchAttribute(string typeName, string methodName, MethodType type = MethodType.Postfix)
        {
            TypeName = typeName;
            Type = null;
            MethodName = methodName;
            PatchType = type;
        }

        /// <summary>Targets a method of a compile-time-resolved type (e.g. an osu.Framework type from the plugin's reference assemblies).</summary>
        public OsuCcPatchAttribute(Type type, string methodName, MethodType patchType = MethodType.Postfix)
        {
            TypeName = null;
            Type = type ?? throw new ArgumentNullException(nameof(type));
            MethodName = methodName;
            PatchType = patchType;
        }

        /// <summary>Full name of the runtime osu.Game type to resolve, or <c>null</c> when targeting <see cref="Type"/>.</summary>
        public string? TypeName { get; }

        /// <summary>Compile-time target type, or <c>null</c> when targeting <see cref="TypeName"/>.</summary>
        public Type? Type { get; }

        /// <summary>Name of the method to patch (e.g. <c>load</c>, <c>set_Text</c>, <c>Perform</c>).</summary>
        public string MethodName { get; }

        /// <summary>Which hook type is applied; also selects the patch method name on the class.</summary>
        public MethodType PatchType { get; }
    }
}
