using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace osucc.App.Updater;

/// <summary>
/// Updates the launcher itself. A global dotnet tool goes through <c>dotnet tool update</c>;
/// a standalone binary is swapped for the release build of the matching OS (Windows uses a
/// deferred renamer because the exe is locked while the process is running).
/// </summary>
internal static class LauncherUpdater
{
    private const string repository = "rus07tam/osu-cc";

    public static async Task<int> UpdateAsync(HttpClient http)
        => IsDotnetTool() ? UpdateTool() : await UpdateStandaloneAsync(http);

    private static bool IsDotnetTool()
    {
        string toolsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools");
        string store = Path.Combine(toolsRoot, ".store");

        return AppContext.BaseDirectory.StartsWith(store, StringComparison.OrdinalIgnoreCase);
    }

    private static int UpdateTool()
    {
        Console.WriteLine("Updating the osucc dotnet tool...");
        return ProcessRunner.Run("dotnet", "tool", "update", "--global", "osucc");
    }

    private static async Task<int> UpdateStandaloneAsync(HttpClient http)
    {
        string? currentExecutable = Environment.ProcessPath;

        if (currentExecutable == null)
        {
            Console.Error.WriteLine("ERROR: cannot determine the current executable path.");
            return 1;
        }

        (string Tag, string Url)? latest = await GetLatestStandaloneAsync(http);

        if (latest == null)
        {
            Console.Error.WriteLine("ERROR: cannot reach GitHub to find the latest launcher.");
            return 1;
        }

        if (NormalizeVersion(latest.Value.Tag) == CurrentVersion())
        {
            Console.WriteLine($"Launcher already up to date ({latest.Value.Tag}).");
            return 0;
        }

        Console.WriteLine($"Downloading {latest.Value.Url}...");
        string newPath = currentExecutable + ".new";

        using (HttpResponseMessage response = await http.GetAsync(latest.Value.Url))
        {
            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine("ERROR: failed to download the launcher update.");
                return 1;
            }

            await using (FileStream stream = File.Create(newPath))
                await response.Content.CopyToAsync(stream);
        }

        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(newPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

        if (OperatingSystem.IsWindows())
        {
            // The running exe is locked: a detached script replaces it after this process exits.
            string? directory = Path.GetDirectoryName(currentExecutable);
            string scriptPath = Path.Combine(directory ?? Path.GetTempPath(), $"osucc-update-{Guid.NewGuid():N}.cmd");

            await File.WriteAllTextAsync(scriptPath,
                $"@echo off\r\nping 127.0.0.1 -n 3 >nul\r\nmove /y \"{newPath}\" \"{currentExecutable}\" >nul\r\ndel \"{scriptPath}\"\r\n");

            Process.Start(new ProcessStartInfo(scriptPath) { UseShellExecute = true });
        }
        else
        {
            // Replacing a running binary works on Linux: the rename detaches the old inode.
            File.Move(newPath, currentExecutable, overwrite: true);
        }

        Console.WriteLine("Launcher updated; restart osucc to use the new version.");
        return 0;
    }

    private static async Task<(string Tag, string Url)?> GetLatestStandaloneAsync(HttpClient http)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"https://api.github.com/repos/{repository}/releases/latest");
        request.Headers.UserAgent.ParseAdd("osucc-updater");

        using HttpResponseMessage response = await http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        string assetName = OperatingSystem.IsWindows() ? "osucc.exe" : "osucc";
        string tag = document.RootElement.GetProperty("tag_name").GetString() ?? string.Empty;

        foreach (JsonElement asset in document.RootElement.GetProperty("assets").EnumerateArray())
        {
            if ((asset.GetProperty("name").GetString() ?? string.Empty) == assetName)
                return (tag, asset.GetProperty("browser_download_url").GetString() ?? string.Empty);
        }

        return null;
    }

    /// <summary>Drops the git-source <c>+sha</c> suffix and a leading <c>v</c>, e.g. <c>1.0.0+abc</c> → <c>1.0.0</c>.</summary>
    private static string NormalizeVersion(string version)
    {
        int plus = version.IndexOf('+');
        string clean = plus >= 0 ? version[..plus] : version;
        return clean.TrimStart('v');
    }

    private static string CurrentVersion()
    {
        string? informational = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return NormalizeVersion(informational ?? string.Empty);
    }
}
