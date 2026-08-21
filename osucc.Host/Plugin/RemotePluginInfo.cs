using osucc.Plugin;

namespace osucc.Plugin;

public sealed class RemotePluginInfo : IPluginMetadata
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Version { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public string? IconPath { get; init; }
    public string? IconResource { get; init; }
    public string? Repository { get; init; }
    public IReadOnlyList<PluginAuthor> Authors { get; init; } = Array.Empty<PluginAuthor>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<PluginDocument> Documents { get; init; } = Array.Empty<PluginDocument>();
    public string RepoFullName { get; init; } = string.Empty;
    public int Stars { get; init; }
}
