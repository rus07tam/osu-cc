using System;

namespace osucc.Core
{
    /// <summary>
    /// Declaratively marks a class as a postfix patch for a constructor of a runtime osu.Game
    /// type. Parameter types disambiguate between overloads. The patch method is inferred by name:
    /// <c>Postfix</c>. Like <see cref="OsuCcPatchAttribute"/>, no manual <c>Install</c> method is
    /// needed and a static settable property/field of type
    /// <see cref="osucc.Plugin.IOsuCcPluginHost"/> is assigned before installation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class OsuCcConstructorPatchAttribute : Attribute
    {
        /// <summary>Marks the class as a postfix patch for the matching constructor of the runtime osu.Game type.</summary>
        public OsuCcConstructorPatchAttribute(string typeName, params Type[] parameterTypes)
        {
            TypeName = typeName;
            ParameterTypes = parameterTypes;
        }

        /// <summary>Full name of the runtime osu.Game type whose constructor is patched.</summary>
        public string TypeName { get; }

        /// <summary>Parameter types of the constructor to patch (empty for the parameterless one).</summary>
        public Type[] ParameterTypes { get; }
    }
}
