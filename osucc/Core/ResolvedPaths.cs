namespace osucc.App;

/// <summary>Everything a command needs, resolved once from the command-line options.</summary>
public sealed record ResolvedPaths(
    string OsuDirectory,
    string OsuCcDirectory,
    string HookDirectory,
    string PluginsDirectory,
    string StagingDirectory);
