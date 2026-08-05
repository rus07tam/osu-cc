using DotMake.CommandLine;
using osucc.Common;

namespace osucc.App.Commands;

/// <summary>Shows resolved paths, the deployed hook/plugin state and any staged update.</summary>
[CliCommand(Description = "Show resolved paths, deployed hook/plugin state and any staged update.")]
public class StatusCommand
{
    public RootCliCommand Root { get; set; } = null!;

    public int Run()
    {
        var paths = Root.ResolvePaths();
        string hookDll = Path.Combine(paths.HookDirectory, OsuCcLayout.HookDllName);
        string? hookVersion = OsuCcVersionReader.Read(hookDll);

        Console.WriteLine($"osu install   : {paths.OsuDirectory} ({(File.Exists(OsuCcPaths.ResolveExecutable(paths.OsuDirectory)) ? "osu! found" : "no osu! executable")})");
        Console.WriteLine($"osu-cc data   : {paths.OsuCcDirectory}");
        Console.WriteLine($"Hook dir      : {paths.HookDirectory}");
        Console.WriteLine($"Hook version  : {hookVersion ?? "(none)"}");

        Console.WriteLine($"Plugins dir   : {paths.PluginsDirectory}");

        string[] archives = Directory.Exists(paths.PluginsDirectory)
            ? Directory.GetFiles(paths.PluginsDirectory, "*.zip").Select(name => Path.GetFileName(name)!).OrderBy(n => n, StringComparer.Ordinal).ToArray()
            : Array.Empty<string>();

        if (archives.Length == 0)
        {
            Console.WriteLine("Plugins       : (none)");
        }
        else
        {
            foreach (string archive in archives)
                Console.WriteLine($"  plugin      : {archive}");
        }

        var marker = UpdateMarker.TryRead(paths.StagingDirectory);

        if (marker != null)
            Console.WriteLine($"Update        : v{marker.Version} staged ({marker.Source}) - run 'osucc start' to apply");

        return 0;
    }
}
