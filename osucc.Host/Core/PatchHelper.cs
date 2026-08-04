using osucc.Plugin;
using System;
using System.Reflection;

namespace osucc.Core
{
    /// <summary>
    /// Thin wrapper over <see cref="Reflection"/> + the plugin host for the common plugin patch
    /// shape: resolve the target by name against the runtime osu.Game assembly and let the host
    /// apply it (see <see cref="IOsuCcPluginHost.AddPatch"/>). Returns <c>null</c> when the
    /// target is not found, and the handle's dispose reverts the patch.
    /// </summary>
    public static class PatchHelper
    {
        /// <summary>Attaches a prefix patch to a method resolved by name. <c>null</c> if the target does not exist.</summary>
        public static IDisposable? AttachPrefix(IOsuCcPluginHost host, string typeName, string methodName, Type patchType, string patchMethodName)
            => attach(host, typeName, methodName, patchType, patchMethodName, MethodType.Prefix);

        /// <summary>Attaches a postfix patch to a method resolved by name. <c>null</c> if the target does not exist.</summary>
        public static IDisposable? AttachPostfix(IOsuCcPluginHost host, string typeName, string methodName, Type patchType, string patchMethodName)
            => attach(host, typeName, methodName, patchType, patchMethodName, MethodType.Postfix);

        /// <summary>Attaches a transpiler to a method resolved by name. <c>null</c> if the target does not exist.</summary>
        public static IDisposable? AttachTranspiler(IOsuCcPluginHost host, string typeName, string methodName, Type patchType, string patchMethodName)
            => attach(host, typeName, methodName, patchType, patchMethodName, MethodType.Transpiler);

        /// <summary>Attaches a postfix patch to an already-resolved method (e.g. a <c>typeof(...)</c> reference to a non-osu.Game type). <c>null</c> if it cannot be patched.</summary>
        public static IDisposable? AttachMethodPostfix(IOsuCcPluginHost host, MethodBase target, Type patchType, string patchMethodName)
            => host.AddPatch(target, patchType, patchMethodName, MethodType.Postfix);

        /// <summary>Attaches a postfix patch to a constructor resolved by signature. <c>null</c> if the target does not exist.</summary>
        public static IDisposable? AttachConstructorPostfix(IOsuCcPluginHost host, string typeName, Type patchType, string patchMethodName, params Type[] ctorParameterTypes)
        {
            var constructor = Reflection.GetConstructor(typeName, ctorParameterTypes);
            return constructor == null ? null : host.AddPatch(constructor, patchType, patchMethodName, MethodType.Postfix);
        }

        private static IDisposable? attach(IOsuCcPluginHost host, string typeName, string methodName, Type patchType, string patchMethodName, MethodType type)
        {
            var method = Reflection.GetMethod(typeName, methodName);
            return method == null ? null : host.AddPatch(method, patchType, patchMethodName, type);
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
