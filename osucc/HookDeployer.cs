namespace osucc.App;

/// <summary>
/// Copies the hook payload (<c>osucc.dll</c> + <c>0Harmony.dll</c> + <c>SharpCompress.dll</c>)
/// from the build output into the osu-cc data root's <c>hook</c> folder. The osu install dir is
/// never touched.
/// </summary>
internal static class HookDeployer
{
    private static readonly string[] hookFiles = { "osucc.dll", "0Harmony.dll", "SharpCompress.dll" };

    /// <summary>Returns false if a required hook file is missing from the build output or could not be copied.</summary>
    public static bool Deploy(string repoRoot, string config, string hookDirectory)
    {
        string output = OsuCcPaths.ResolveHookOutput(repoRoot, config);
        Directory.CreateDirectory(hookDirectory);

        foreach (string name in hookFiles)
        {
            string source = Path.Combine(output, name);
            string target = Path.Combine(hookDirectory, name);

            if (!File.Exists(source))
            {
                Console.Error.WriteLine($"ERROR: {name} not found in {output} - run 'osucc build' first.");
                return false;
            }

            File.Copy(source, target, overwrite: true);
            Console.WriteLine($"Deployed {name} -> {target}");
        }

        return true;
    }
}
