using System;
using System.IO;

namespace osucc.App;

/// <summary>
/// Builds the hook and the plugins by delegating to the repo's single MSBuild entry point
/// (<c>osucc.build.proj</c>), which packs osucc.Host/osucc.Build into the local feed, clears their
/// stale global-cache copies and builds everything in one (parallel) MSBuild process.
/// </summary>
internal static class BuildRunner
{
    /// <summary>Runs the orchestrator build, returning its exit code (0 on success).</summary>
    public static int Build(string? repoRoot, string config)
    {
        if (repoRoot == null)
        {
            Console.Error.WriteLine("ERROR: cannot locate repo root (osucc.sln). Pass --repo.");
            return 1;
        }

        string project = Path.Combine(repoRoot, "osucc.build.proj");

        if (!File.Exists(project))
        {
            Console.Error.WriteLine($"ERROR: {project} not found - not an osu-cc repo root?");
            return 1;
        }

        return ProcessRunner.Run("dotnet", "build", project, "-c", config, "--nologo", "-v", "m");
    }
}
