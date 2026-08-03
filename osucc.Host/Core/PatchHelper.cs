using HarmonyLib;
using System.Reflection;

namespace osucc.Core
{
    /// <summary>
    /// Thin wrapper over <see cref="Reflection"/> + Harmony for the common plugin patch shape:
    /// resolve the target by name against the runtime osu.Game assembly and skip (return
    /// <c>false</c>) when it is not found. Removes the per-patch Install boilerplate.
    /// </summary>
    public static class PatchHelper
    {
        /// <summary>Attaches a prefix patch to a method resolved by name. Returns <c>false</c> if the target does not exist.</summary>
        public static bool AttachPrefix(Harmony harmony, string typeName, string methodName, Type patchType, string patchMethodName)
            => attach(harmony, typeName, methodName, patchType, patchMethodName, MethodType.Prefix);

        /// <summary>Attaches a postfix patch to a method resolved by name. Returns <c>false</c> if the target does not exist.</summary>
        public static bool AttachPostfix(Harmony harmony, string typeName, string methodName, Type patchType, string patchMethodName)
            => attach(harmony, typeName, methodName, patchType, patchMethodName, MethodType.Postfix);

        /// <summary>Attaches a transpiler to a method resolved by name. Returns <c>false</c> if the target does not exist.</summary>
        public static bool AttachTranspiler(Harmony harmony, string typeName, string methodName, Type patchType, string patchMethodName)
            => attach(harmony, typeName, methodName, patchType, patchMethodName, MethodType.Transpiler);

        private static bool attach(Harmony harmony, string typeName, string methodName, Type patchType, string patchMethodName, MethodType type)
        {
            var method = Reflection.GetMethod(typeName, methodName);
            if (method == null)
                return false;

            var patch = Reflection.HarmonyMethod(patchType, patchMethodName);

            harmony.Patch(method,
                prefix: type == MethodType.Prefix ? patch : null,
                postfix: type == MethodType.Postfix ? patch : null,
                transpiler: type == MethodType.Transpiler ? patch : null);
            return true;
        }

        /// <summary>Attaches a postfix patch to a constructor resolved by signature. Returns <c>false</c> if the target does not exist.</summary>
        public static bool AttachConstructorPostfix(Harmony harmony, string typeName, Type patchType, string patchMethodName, params Type[] ctorParameterTypes)
        {
            var constructor = Reflection.GetConstructor(typeName, ctorParameterTypes);
            if (constructor == null)
                return false;

            harmony.Patch(constructor, postfix: Reflection.HarmonyMethod(patchType, patchMethodName));
            return true;
        }
    }

    /// <summary>Which Harmony patch type <see cref="PatchHelper"/> should attach.</summary>
    public enum MethodType
    {
        Prefix,
        Postfix,
        Transpiler
    }
}
