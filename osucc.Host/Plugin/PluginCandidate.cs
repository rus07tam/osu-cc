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

        /// <summary>Structured declared dependencies.</summary>
        public IReadOnlyList<PluginDependencyDeclaration> DependencyDeclarations => Metadata.ResolveDependencyDeclarations();

        /// <summary>Diagnostics collected during scanning and dependency validation.</summary>
        public List<PluginDiagnostic> Diagnostics { get; } = new();

        /// <summary>Whether this candidate is blocked from loading due to host version incompatibility.</summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Whether the plugin will actually load this launch: enabled, not blocked by incompatibilities, and built against a
        /// supported API version. Only loadable candidates take part in dependency ordering.
        /// </summary>
        public bool IsLoadable => !IsBlocked && PluginStateStore.IsEnabled(Metadata.Id);
    }
}
