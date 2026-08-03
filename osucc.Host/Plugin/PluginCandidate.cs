namespace osucc.Plugin
{
    /// <summary>
    /// A discovered <c>[OsuCcPlugin]</c> type, awaiting instantiation in dependency-resolved
    /// priority order. Created during discovery without running any plugin code.
    /// </summary>
    internal sealed record PluginCandidate(Type Type, OsuCcPluginAttribute Attribute, string Directory, string? IconPath)
    {
        /// <summary>Load/attach priority: a persisted override wins over the attribute's value.</summary>
        public int EffectivePriority => PluginStateStore.GetPriority(Attribute.Id) ?? Attribute.Priority;

        /// <summary>Stable ids this plugin depends on (see <see cref="OsuCcPluginAttribute.DependsOn"/>).</summary>
        public IReadOnlyList<string> Dependencies => Attribute.DependsOn;

        /// <summary>
        /// Whether the plugin will actually load this launch: enabled and built against a
        /// supported API version. Only loadable candidates take part in dependency ordering.
        /// </summary>
        public bool IsLoadable => PluginStateStore.IsEnabled(Attribute.Id) && Attribute.ApiVersion == OsuCcPluginAttribute.CurrentApiVersion;
    }
}
