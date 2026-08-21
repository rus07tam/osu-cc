namespace osucc.Common;

/// <summary>
/// The canonical on-disk layout of the osu-cc data root: the <c>hook</c> folder holding the
/// startup-hook payload, the <c>plugins</c> folder holding plugin archives and the transient
/// <c>staging</c> folder used by the launcher. Every osu-cc surface (launcher, hook, build)
/// agrees on these names.
/// <para>
/// Plugin archives follow the naming scheme <c>plugin-{id}-{version}.zip</c>. Runtime bundles
/// are named <c>runtime-{version}.zip</c> and bootstrap bundles <c>bootstrap-{version}.zip</c>.
/// </para>
/// </summary>
public static class OsuCcLayout
{
    /// <summary>Name of the osu-cc data root folder inside the game's data folder.</summary>
    public const string OsuCcDirectoryName = "osu-cc";

    /// <summary>Folder (under the data root) holding the startup-hook payload: <c>osucc.dll</c> plus its runtime blobs.</summary>
    public const string HookDirectoryName = "hook";

    /// <summary>Folder (under the data root) scanned for plugin archives.</summary>
    public const string PluginsDirectoryName = "plugins";

    /// <summary>Transient folder (under the data root) where the launcher stages the next build before applying it.</summary>
    public const string StagingDirectoryName = "staging";

    /// <summary>Marker file inside <see cref="StagingDirectoryName"/> describing the staged update.</summary>
    public const string UpdateMarkerFileName = "update.json";

    /// <summary>Name of the startup-hook assembly (osucc.Host builds as <c>osucc</c>).</summary>
    public const string HookDllName = "osucc.dll";

    /// <summary>Runtime blobs that must sit next to <see cref="HookDllName"/> in the hook folder (osu.Game provides the osu.* assemblies).</summary>
    public static readonly string[] HookRuntimeBlobs =
    {
        "0Harmony.dll",
        "SharpCompress.dll",
        "osucc.Shared.dll",
    };

    /// <summary>Every file that makes up the hook payload.</summary>
    public static readonly string[] HookFiles = new[] { HookDllName }.Concat(HookRuntimeBlobs).ToArray();

    /// <summary>Versioned plugin archive name: <c>plugin-{pluginId}-{version}.zip</c>.</summary>
    public static string PluginArchiveName(string pluginId, string version)
        => $"plugin-{pluginId}-{version}.zip";

    /// <summary>Prefix shared by all archives of a given plugin: <c>plugin-{pluginId}-</c>.</summary>
    public static string PluginArchivePrefix(string pluginId) => $"plugin-{pluginId}-";

    /// <summary>Common prefix of all runtime bundle filenames.</summary>
    public const string RuntimeBundlePrefix = "runtime-";

    /// <summary>Common prefix of all bootstrap bundle filenames.</summary>
    public const string BootstrapBundlePrefix = "bootstrap-";

    /// <summary>Runtime bundle archive name: <c>runtime-{version}.zip</c>.</summary>
    public static string RuntimeBundleName(string version) => $"runtime-{version}.zip";

    /// <summary>Bootstrap bundle archive name: <c>bootstrap-{version}.zip</c>.</summary>
    public static string BootstrapBundleName(string version) => $"bootstrap-{version}.zip";

    /// <summary>Path of the <c>staging</c> folder under the given data root.</summary>
    public static string StagingDirectory(string osuCcDirectory) => Path.Combine(osuCcDirectory, StagingDirectoryName);

    /// <summary>Path of the update marker inside the given staging folder.</summary>
    public static string UpdateMarkerPath(string stagingDirectory) => Path.Combine(stagingDirectory, UpdateMarkerFileName);
}
