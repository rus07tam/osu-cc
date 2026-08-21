using System;
using System.Reflection;

namespace osucc.Core
{
    /// <summary>
    /// Describes the method, constructor or property targeted by a patch.
    /// </summary>
    public sealed class PatchTarget
    {
        public string? TypeName { get; }
        public Type? TargetType { get; }
        public string? MethodName { get; }
        public Func<MethodInfo, bool>? MethodPredicate { get; }
        public Type[]? ConstructorParameters { get; }
        public MethodType PatchType { get; }

        public PatchTarget(string typeName, string methodName, MethodType patchType = MethodType.Postfix)
        {
            TypeName = typeName;
            MethodName = methodName;
            PatchType = patchType;
        }

        public PatchTarget(string typeName, string methodName, Func<MethodInfo, bool> methodPredicate, MethodType patchType = MethodType.Postfix)
        {
            TypeName = typeName;
            MethodName = methodName;
            MethodPredicate = methodPredicate;
            PatchType = patchType;
        }

        public PatchTarget(Type targetType, string methodName, MethodType patchType = MethodType.Postfix)
        {
            TargetType = targetType;
            MethodName = methodName;
            PatchType = patchType;
        }

        public PatchTarget(Type targetType, string methodName, Func<MethodInfo, bool> methodPredicate, MethodType patchType = MethodType.Postfix)
        {
            TargetType = targetType;
            MethodName = methodName;
            MethodPredicate = methodPredicate;
            PatchType = patchType;
        }

        public PatchTarget(string typeName, Type[] constructorParameters)
        {
            TypeName = typeName;
            ConstructorParameters = constructorParameters;
            PatchType = MethodType.Postfix;
        }

        public PatchTarget(Type targetType, Type[] constructorParameters)
        {
            TargetType = targetType;
            ConstructorParameters = constructorParameters;
            PatchType = MethodType.Postfix;
        }

        public MethodBase? Resolve()
        {
            if (TargetType != null)
            {
                if (ConstructorParameters != null)
                    return TargetType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, ConstructorParameters, null);

                if (MethodName != null)
                {
                    if (MethodPredicate != null)
                    {
                        var methods = TargetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var m in methods)
                        {
                            if (m.Name == MethodName && MethodPredicate(m))
                                return m;
                        }
                    }

                    return TargetType.GetMethod(MethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                }
            }

            if (!string.IsNullOrEmpty(TypeName))
            {
                if (ConstructorParameters != null)
                    return Reflection.GetConstructor(TypeName, ConstructorParameters);

                if (MethodName != null)
                {
                    return MethodPredicate != null
                        ? Reflection.GetMethod(TypeName, MethodName, MethodPredicate)
                        : Reflection.GetMethod(TypeName, MethodName);
                }
            }

            return null;
        }

        public string Describe
            => TargetType != null
                ? (ConstructorParameters != null ? $"{TargetType.FullName}..ctor" : $"{TargetType.FullName}.{MethodName}")
                : (ConstructorParameters != null ? $"{TypeName}..ctor" : $"{TypeName}.{MethodName}");
    }
}
