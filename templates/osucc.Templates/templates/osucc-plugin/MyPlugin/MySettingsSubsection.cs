using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;
using osucc.Plugin;

namespace MyPlugin;

/// <summary>
/// Settings subsection injected into the "Specials" section; every control binds to the
/// plugin's own <see cref="PluginSettings"/>.
/// </summary>
public partial class MySettingsSubsection : SettingsSubsection
{
    protected override LocalisableString Header => MyPluginStrings.Name;

    public MySettingsSubsection(PluginSettings settings)
    {
        this.AddCheckbox(settings, "enabled", true, MyPluginStrings.EnabledCaption, MyPluginStrings.EnabledHint);
    }
}
