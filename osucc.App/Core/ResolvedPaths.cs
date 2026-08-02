namespace osucc.App;

/// <summary>
/// Everything a command needs, resolved once from the command-line options. <see cref="RepoRoot"/>
/// is null when no local checkout was found and the command does not require one.
/// </summary>
public sealed record ResolvedPaths(string Config, string? RepoRoot, string OsuDirectory, string HookDirectory, string PluginsDirectory);
