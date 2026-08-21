using osucc.Core;
using System;
using System.Reflection;

namespace osucc.Plugin
{
    /// <summary>
    /// Base class for plugin-specific Harmony patches.
    /// Automatically gates execution based on <see cref="OsuCcPlugin.Enabled"/> and routes logging to <see cref="IOsuCcPluginHost"/>.
    /// </summary>
    public abstract class PluginPatch : OsuCcPatch
    {
        /// <summary>The plugin instance owning this patch.</summary>
        public OsuCcPlugin Plugin { get; }

        /// <summary>The scoped host provided to the plugin.</summary>
        public IOsuCcPluginHost Host { get; }

        /// <summary>
        /// Gated by plugin enabled state by default.
        /// Can be overridden to chain additional conditions: <c>base.Condition &amp;&amp; MyFeature.Enabled</c>.
        /// </summary>
        public override bool Condition => Plugin.Enabled;

        protected PluginPatch(OsuCcPlugin plugin, IOsuCcPluginHost host, string typeName, string methodName, MethodType patchType = MethodType.Postfix)
            : base(typeName, methodName, patchType)
        {
            Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        protected PluginPatch(OsuCcPlugin plugin, IOsuCcPluginHost host, string typeName, string methodName, Func<MethodInfo, bool> methodPredicate, MethodType patchType = MethodType.Postfix)
            : base(typeName, methodName, methodPredicate, patchType)
        {
            Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        protected PluginPatch(OsuCcPlugin plugin, IOsuCcPluginHost host, Type type, string methodName, MethodType patchType = MethodType.Postfix)
            : base(type, methodName, patchType)
        {
            Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        protected PluginPatch(OsuCcPlugin plugin, IOsuCcPluginHost host, Type type, string methodName, Func<MethodInfo, bool> methodPredicate, MethodType patchType = MethodType.Postfix)
            : base(type, methodName, methodPredicate, patchType)
        {
            Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        protected PluginPatch(OsuCcPlugin plugin, IOsuCcPluginHost host, string typeName, Type[] constructorParameters)
            : base(typeName, constructorParameters)
        {
            Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        protected PluginPatch(OsuCcPlugin plugin, IOsuCcPluginHost host, Type type, Type[] constructorParameters)
            : base(type, constructorParameters)
        {
            Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        protected PluginPatch(OsuCcPlugin plugin, IOsuCcPluginHost host, PatchTarget target)
            : base(target)
        {
            Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public override void LogError(string message, Exception? ex = null)
        {
            Host.Log(LogLevel.Error, ex != null ? $"[{Name}] {message}: {ex}" : $"[{Name}] {message}");
        }

        public override void LogInfo(string message)
        {
            Host.Log(LogLevel.Info, $"[{Name}] {message}");
        }
    }

    /// <summary>
    /// Strongly-typed base class for plugin Harmony patches.
    /// </summary>
    public abstract class PluginPatch<TPlugin> : PluginPatch where TPlugin : OsuCcPlugin
    {
        public new TPlugin Plugin => (TPlugin)base.Plugin;

        protected PluginPatch(TPlugin plugin, IOsuCcPluginHost host, string typeName, string methodName, MethodType patchType = MethodType.Postfix)
            : base(plugin, host, typeName, methodName, patchType)
        {
        }

        protected PluginPatch(TPlugin plugin, IOsuCcPluginHost host, string typeName, string methodName, Func<MethodInfo, bool> methodPredicate, MethodType patchType = MethodType.Postfix)
            : base(plugin, host, typeName, methodName, methodPredicate, patchType)
        {
        }

        protected PluginPatch(TPlugin plugin, IOsuCcPluginHost host, Type type, string methodName, MethodType patchType = MethodType.Postfix)
            : base(plugin, host, type, methodName, patchType)
        {
        }

        protected PluginPatch(TPlugin plugin, IOsuCcPluginHost host, Type type, string methodName, Func<MethodInfo, bool> methodPredicate, MethodType patchType = MethodType.Postfix)
            : base(plugin, host, type, methodName, methodPredicate, patchType)
        {
        }

        protected PluginPatch(TPlugin plugin, IOsuCcPluginHost host, string typeName, Type[] constructorParameters)
            : base(plugin, host, typeName, constructorParameters)
        {
        }

        protected PluginPatch(TPlugin plugin, IOsuCcPluginHost host, Type type, Type[] constructorParameters)
            : base(plugin, host, type, constructorParameters)
        {
        }

        protected PluginPatch(TPlugin plugin, IOsuCcPluginHost host, PatchTarget target)
            : base(plugin, host, target)
        {
        }
    }
}
