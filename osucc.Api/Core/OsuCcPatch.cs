using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace osucc.Core
{
    /// <summary>
    /// Base class for all osu!cc and plugin Harmony patches.
    /// Encapsulates target resolution, runtime condition gating, and automatic try/catch error logging.
    /// </summary>
    public abstract class OsuCcPatch
    {
        private static readonly AssemblyBuilder dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("OsuCc.DynamicPatches"),
            AssemblyBuilderAccess.Run);

        private static readonly ModuleBuilder dynamicModule = dynamicAssembly.DefineDynamicModule("OsuCc.DynamicPatches");

        private static readonly ConcurrentDictionary<int, OsuCcPatch> patchRegistry = new();
        private static int nextPatchId;

        public int PatchId { get; }

        /// <summary>Target method, constructor or property description.</summary>
        public PatchTarget Target { get; }

        /// <summary>
        /// Computable condition: when false, the patch logic is bypassed.
        /// Prefix returns true (original executes), Postfix/Finalizer no-ops.
        /// </summary>
        public virtual bool Condition => true;

        /// <summary>Display/log name of this patch (defaults to class name).</summary>
        public virtual string Name => GetType().Name;

        protected OsuCcPatch(string typeName, string methodName, MethodType patchType = MethodType.Postfix)
            : this(new PatchTarget(typeName, methodName, patchType))
        {
        }

        protected OsuCcPatch(string typeName, string methodName, Func<MethodInfo, bool> methodPredicate, MethodType patchType = MethodType.Postfix)
            : this(new PatchTarget(typeName, methodName, methodPredicate, patchType))
        {
        }

        protected OsuCcPatch(Type type, string methodName, MethodType patchType = MethodType.Postfix)
            : this(new PatchTarget(type, methodName, patchType))
        {
        }

        protected OsuCcPatch(Type type, string methodName, Func<MethodInfo, bool> methodPredicate, MethodType patchType = MethodType.Postfix)
            : this(new PatchTarget(type, methodName, methodPredicate, patchType))
        {
        }

        protected OsuCcPatch(string typeName, Type[] constructorParameters)
            : this(new PatchTarget(typeName, constructorParameters))
        {
        }

        protected OsuCcPatch(Type type, Type[] constructorParameters)
            : this(new PatchTarget(type, constructorParameters))
        {
        }

        protected OsuCcPatch(PatchTarget target)
        {
            Target = target;
            PatchId = System.Threading.Interlocked.Increment(ref nextPatchId);
            patchRegistry[PatchId] = this;
        }

        public static OsuCcPatch? GetPatchInstance(int id)
        {
            patchRegistry.TryGetValue(id, out var patch);
            return patch;
        }

        public virtual void LogError(string message, Exception? ex = null)
        {
            TimingLog.Error(ex != null ? $"[{Name}] {message}: {ex}" : $"[{Name}] {message}");
        }

        public virtual void LogInfo(string message)
        {
            TimingLog.Info($"[{Name}] {message}");
        }

        /// <summary>
        /// Installs this patch into the specified Harmony instance.
        /// Automatically binds patch methods (Prefix, Postfix, Transpiler, Finalizer),
        /// wraps them in condition gating and try/catch protection, and registers them.
        /// </summary>
        public virtual bool Install(Harmony harmony)
        {
            var targetMethod = Target.Resolve();
            if (targetMethod == null)
            {
                LogError($"Target method not found ({Target.Describe})");
                return false;
            }

            var prefixMethod = findPatchMethod(MethodType.Prefix);
            var postfixMethod = findPatchMethod(MethodType.Postfix);
            var transpilerMethod = findPatchMethod(MethodType.Transpiler);
            var finalizerMethod = findPatchMethod(MethodType.Finalizer);

            if (prefixMethod == null && postfixMethod == null && transpilerMethod == null && finalizerMethod == null)
            {
                LogError($"No patch method (Prefix/Postfix/Transpiler/Finalizer) found on {GetType().FullName}");
                return false;
            }

            try
            {
                HarmonyMethod? prefix = prefixMethod != null ? new HarmonyMethod(createWrappedMethod(prefixMethod, MethodType.Prefix)) : null;
                HarmonyMethod? postfix = postfixMethod != null ? new HarmonyMethod(createWrappedMethod(postfixMethod, MethodType.Postfix)) : null;
                HarmonyMethod? transpiler = transpilerMethod != null ? new HarmonyMethod(createWrappedMethod(transpilerMethod, MethodType.Transpiler)) : null;
                HarmonyMethod? finalizer = finalizerMethod != null ? new HarmonyMethod(createWrappedMethod(finalizerMethod, MethodType.Finalizer)) : null;

                harmony.Patch(targetMethod, prefix: prefix, postfix: postfix, transpiler: transpiler, finalizer: finalizer);

                LogInfo($"Patched {Target.Describe}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to attach patch to {Target.Describe}", ex);
                return false;
            }
        }

        private MethodInfo? findPatchMethod(MethodType type)
        {
            string expectedName = type.ToString();
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var methods = GetType().GetMethods(flags);

            // First check by exact name (Prefix, Postfix, Transpiler, Finalizer)
            var match = methods.FirstOrDefault(m => string.Equals(m.Name, expectedName, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;

            // Next check for Harmony attributes if present
            foreach (var m in methods)
            {
                if (type == MethodType.Prefix && m.GetCustomAttributes().Any(a => a.GetType().Name == "HarmonyPrefixAttribute"))
                    return m;
                if (type == MethodType.Postfix && m.GetCustomAttributes().Any(a => a.GetType().Name == "HarmonyPostfixAttribute"))
                    return m;
                if (type == MethodType.Transpiler && m.GetCustomAttributes().Any(a => a.GetType().Name == "HarmonyTranspilerAttribute"))
                    return m;
                if (type == MethodType.Finalizer && m.GetCustomAttributes().Any(a => a.GetType().Name == "HarmonyFinalizerAttribute"))
                    return m;
            }

            return null;
        }

        private MethodInfo createWrappedMethod(MethodInfo userMethod, MethodType kind)
        {
            if (kind == MethodType.Transpiler)
                return userMethod;

            var parameters = userMethod.GetParameters();
            var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
            var returnType = userMethod.ReturnType;

            string typeName = $"Wrapper_{GetType().Name}_{kind}_{userMethod.Name}_{PatchId}_{Guid.NewGuid():N}";
            var typeBuilder = dynamicModule.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed);

            var methodBuilder = typeBuilder.DefineMethod(
                userMethod.Name,
                MethodAttributes.Public | MethodAttributes.Static,
                returnType,
                parameterTypes);

            for (int i = 0; i < parameters.Length; i++)
            {
                methodBuilder.DefineParameter(i + 1, parameters[i].Attributes, parameters[i].Name);
            }

            var il = methodBuilder.GetILGenerator();
            var getPatchMethod = typeof(OsuCcPatch).GetMethod(nameof(GetPatchInstance), BindingFlags.Public | BindingFlags.Static)!;
            var getConditionMethod = typeof(OsuCcPatch).GetProperty(nameof(Condition))!.GetGetMethod()!;
            var logErrorMethod = typeof(OsuCcPatch).GetMethod(nameof(LogError), new[] { typeof(string), typeof(Exception) })!;

            var runPatchLabel = il.DefineLabel();
            var endLabel = il.DefineLabel();

            LocalBuilder? resultLocal = returnType != typeof(void) ? il.DeclareLocal(returnType) : null;
            LocalBuilder exLocal = il.DeclareLocal(typeof(Exception));

            // 1. Condition check
            il.Emit(OpCodes.Ldc_I4, PatchId);
            il.Emit(OpCodes.Call, getPatchMethod);
            il.Emit(OpCodes.Callvirt, getConditionMethod);
            il.Emit(OpCodes.Brtrue_S, runPatchLabel);

            // Condition is FALSE -> Return bypass
            if (kind == MethodType.Prefix)
            {
                if (returnType == typeof(bool))
                {
                    il.Emit(OpCodes.Ldc_I4_1); // return true (continue original)
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    il.Emit(OpCodes.Ret);
                }
            }
            else if (kind == MethodType.Finalizer)
            {
                // Return original exception (__exception parameter if present)
                int exceptionParamIndex = -1;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].Name == "__exception" || parameters[i].ParameterType == typeof(Exception))
                    {
                        exceptionParamIndex = i;
                        break;
                    }
                }

                if (exceptionParamIndex >= 0)
                    emitLdarg(il, exceptionParamIndex);
                else
                    il.Emit(OpCodes.Ldnull);

                il.Emit(OpCodes.Ret);
            }
            else
            {
                il.Emit(OpCodes.Ret);
            }

            // 2. Run patch with try/catch
            il.MarkLabel(runPatchLabel);
            il.BeginExceptionBlock();

            if (!userMethod.IsStatic)
            {
                il.Emit(OpCodes.Ldc_I4, PatchId);
                il.Emit(OpCodes.Call, getPatchMethod);
                il.Emit(OpCodes.Castclass, GetType());
            }

            for (int i = 0; i < parameters.Length; i++)
                emitLdarg(il, i);

            il.Emit(userMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt, userMethod);
            if (resultLocal != null)
                il.Emit(OpCodes.Stloc, resultLocal);

            il.Emit(OpCodes.Leave_S, endLabel);

            // 3. Catch block
            il.BeginCatchBlock(typeof(Exception));
            il.Emit(OpCodes.Stloc, exLocal);

            il.Emit(OpCodes.Ldc_I4, PatchId);
            il.Emit(OpCodes.Call, getPatchMethod);
            il.Emit(OpCodes.Ldstr, $"{kind} execution error");
            il.Emit(OpCodes.Ldloc, exLocal);
            il.Emit(OpCodes.Callvirt, logErrorMethod);

            if (kind == MethodType.Prefix && returnType == typeof(bool) && resultLocal != null)
            {
                il.Emit(OpCodes.Ldc_I4_1); // On Prefix failure: return true to keep original game logic running
                il.Emit(OpCodes.Stloc, resultLocal);
            }

            il.EndExceptionBlock();

            il.MarkLabel(endLabel);
            if (resultLocal != null)
                il.Emit(OpCodes.Ldloc, resultLocal);

            il.Emit(OpCodes.Ret);

            var createdType = typeBuilder.CreateType();
            return createdType!.GetMethod(userMethod.Name, BindingFlags.Public | BindingFlags.Static)!;
        }

        private static void emitLdarg(ILGenerator il, int index)
        {
            switch (index)
            {
                case 0: il.Emit(OpCodes.Ldarg_0); break;
                case 1: il.Emit(OpCodes.Ldarg_1); break;
                case 2: il.Emit(OpCodes.Ldarg_2); break;
                case 3: il.Emit(OpCodes.Ldarg_3); break;
                default:
                    if (index <= 255)
                        il.Emit(OpCodes.Ldarg_S, (byte)index);
                    else
                        il.Emit(OpCodes.Ldarg, index);
                    break;
            }
        }
    }
}
