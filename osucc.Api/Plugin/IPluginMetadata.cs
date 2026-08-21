namespace osucc.Plugin;

public interface IPluginMetadata
{
    string Id { get; }
    string Name { get; }
    string? Description { get; }
    string Version { get; }
    string? Icon { get; }
    string? IconPath { get; }
    string? IconResource { get; }
    string? Repository { get; }
    IReadOnlyList<PluginAuthor> Authors { get; }
    IReadOnlyList<string> Tags { get; }
    IReadOnlyList<PluginDocument> Documents { get; }
}
