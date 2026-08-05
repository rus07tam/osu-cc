using osucc.Plugin;

namespace MyPlugin;

/// <summary>
/// Minimal osu-cc plugin: registers a toolbar button and a settings subsection (persisted via
/// <see cref="PluginSettings"/>). Builds to <c>bin/&lt;config&gt;/net8.0/MyPlugin.zip</c>;
/// drop that archive into the game's <c>osu-cc/plugins</c> folder.
/// Plugin metadata (id, name, author, description, icon…) is declared in the project file
/// (MyPlugin.csproj); the build turns it into an assembly-level manifest, so no attribute is
/// written here. Dependencies on other plugins are declared as
/// <c>&lt;PluginDependency Include="other-plugin-id" /&gt;</c> items — see the ExamplePlugin
/// for a full walkthrough.
/// </summary>
public class MyPlugin : OsuCcPluginBase
{
    private PluginSettings settings = null!;

    protected override void OnLoad()
    {
        settings = Host.GetSettings();

        Host.AddToolbarButton(() => new MyToolbarButton(Host.Notify));
        Host.AddSettingsSubsection(() => new MySettingsSubsection(settings));

        Host.Log("loaded");
    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        settings?.Dispose();
        base.Dispose();
    }
}
