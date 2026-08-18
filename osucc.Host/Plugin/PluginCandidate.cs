namespace osucc.Plugin
{
    /// <summary>
    /// A discovered plugin type with its metadata (the <see cref="OsuCcPluginAttribute"/> emitted
    /// at build time from the project file), awaiting instantiation in dependency-resolved priority
    /// order. Created during discovery without running any plugin code.
    /// </summary>
    internal sealed record PluginCandidate(Type Type, OsuCcPluginAttribute Metadata, string Directory, string? IconPath)
    {
        /// <summary>Load/attach priority: a persisted override wins over the declared value.</summary>
        public int EffectivePriority => PluginStateStore.GetPriority(Metadata.Id) ?? Metadata.Priority;

        /// <summary>Stable ids this plugin depends on (see <see cref="OsuCcPluginAttribute.DependsOn"/>).</summary>
        public IReadOnlyList<string> Dependencies => Metadata.DependsOn;

        /// <summary>
        /// Whether the plugin will actually load this launch: enabled and built against a
        /// supported API version. Only loadable candidates take part in dependency ordering.
        /// </summary>
        public bool IsLoadable => PluginStateStore.IsEnabled(Metadata.Id) && Metadata.ApiVersion == OsuCcPluginAttribute.CurrentApiVersion;
    }
}
