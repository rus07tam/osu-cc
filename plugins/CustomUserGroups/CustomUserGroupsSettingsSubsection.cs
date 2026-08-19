using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;
using osucc.Plugin;

namespace CustomUserGroups
{
    /// <summary>
    /// Settings subsection injected into the "Specials" section: a master toggle, the custom group
    /// library editor and per-user group overrides, all persisted through <see cref="PluginSettings"/>.
    /// </summary>
    public partial class CustomUserGroupsSettingsSubsection : SettingsSubsection
    {
        protected override LocalisableString Header => CustomUserGroupsStrings.Name;

        public CustomUserGroupsSettingsSubsection(PluginSettings settings, CustomUserGroupsApi api, IOsuCcPluginHost host)
        {
            this.AddCheckbox(settings, "enabled", true, CustomUserGroupsStrings.EnabledCaption, CustomUserGroupsStrings.EnabledHint);

            bool hasVisuals = host.GetApi<object>("username-visuals") != null;

            var colourCheckbox = this.AddCheckbox(settings, "apply_username_colour", true, CustomUserGroupsStrings.ApplyColourCaption,
                hasVisuals ? CustomUserGroupsStrings.ApplyColourHint : CustomUserGroupsStrings.ApplyColourHintMissing);

            if (!hasVisuals)
            {
                colourCheckbox.Current.Value = false;
                colourCheckbox.Current.Disabled = true;
            }

            Add(new CustomUserGroupsGroupEditorSection(api, host));
            Add(new CustomUserGroupsUserOverridesSection(api, host));
        }
    }
}
