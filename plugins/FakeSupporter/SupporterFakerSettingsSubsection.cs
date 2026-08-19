using osu.Framework.Bindables;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;
using osucc.Plugin;

namespace FakeSupporter
{
    /// <summary>
    /// Settings subsection injected into the "Specials" section: a master toggle plus a level
    /// slider (1–10 hearts), persisted through <see cref="PluginSettings"/>, and per-user
    /// supporter overrides for specific users.
    /// </summary>
    public partial class SupporterFakerSettingsSubsection : SettingsSubsection
    {
        protected override LocalisableString Header => SupporterFakerStrings.Name;

        public SupporterFakerSettingsSubsection(PluginSettings settings, SupporterFakerApi api, IOsuCcPluginHost host)
        {
            var supporterEnabled = this.AddCheckbox(settings, "enabled", false, SupporterFakerStrings.EnabledCaption, SupporterFakerStrings.EnabledHint);

            // The slider needs a BindableNumber range (1–10), while the plugin settings expose a
            // plain Bindable<int>; mirror its value both ways. TransferValueOnCommit (as osu's own
            // settings sliders use) keeps LoadComplete from writing the instantaneous value into
            // the Disabled bindable when the fake supporter is switched off — that write would
            // otherwise throw.
            var supporterLevel = new FormSliderBar<int>
            {
                Caption = SupporterFakerStrings.LevelCaption,
                HintText = SupporterFakerStrings.LevelHint,
                TransferValueOnCommit = true,
                Current = new BindableNumber<int>
                {
                    MinValue = 1,
                    MaxValue = 10,
                    Value = settings.Bind("level", 2).Value,
                },
            };
            supporterLevel.Current.BindValueChanged(e => settings.Bind("level", 2).Value = e.NewValue, true);
            Add(new SettingsItemV2(supporterLevel));

            supporterEnabled.Current.BindValueChanged(e => supporterLevel.Current.Disabled = !e.NewValue, true);

            Add(new SupporterFakerUserOverridesSection(api, host));
        }
    }
}
