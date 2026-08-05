using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osucc.Plugin;
using System;

namespace OsuCcUpdater
{
    /// <summary>
    /// Settings subsection for the updater: a live status line, the update source selector, an
    /// auto-check toggle and buttons to stage an update from GitHub or from a local build.
    /// </summary>
    public partial class UpdaterSettingsSubsection : SettingsSubsection
    {
        private readonly OsuCcUpdaterApi api;
        private readonly OsuSpriteText status;
        private readonly SettingsButtonV2 checkButton;
        private readonly SettingsButtonV2 buildButton;

        protected override LocalisableString Header => OsuCcUpdaterStrings.Name;

        public UpdaterSettingsSubsection(OsuCcUpdaterApi api)
        {
            this.api = api;

            status = new OsuSpriteText
            {
                RelativeSizeAxes = Axes.X,
            };

            Add(new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = SettingsPanel.CONTENT_PADDING,
                Child = status,
            });

            Add(new SettingsEnumDropdown<UpdateSource>
            {
                LabelText = OsuCcUpdaterStrings.SourceLabel,
                Current = api.Source,
            });

            var autoCheck = new FormCheckBox
            {
                Caption = OsuCcUpdaterStrings.AutoCheckCaption,
                HintText = OsuCcUpdaterStrings.AutoCheckHint,
                Current = api.AutoCheck,
            };

            Add(new SettingsItemV2(autoCheck));

            checkButton = new SettingsButtonV2
            {
                Text = OsuCcUpdaterStrings.CheckButton,
                Action = () => run(api.Source.Value),
            };

            buildButton = new SettingsButtonV2
            {
                Text = OsuCcUpdaterStrings.BuildButton,
                Action = () => run(UpdateSource.LocalBuild),
            };

            Add(checkButton);
            Add(buildButton);

            api.StateChanged += onStateChanged;
            refresh();
        }

        protected override void Dispose(bool isDisposing)
        {
            api.StateChanged -= onStateChanged;
            base.Dispose(isDisposing);
        }

        private void onStateChanged() => Scheduler.AddOnce(refresh);

        private void refresh()
        {
            if (api.Busy)
            {
                status.Text = api.Source.Value == UpdateSource.LocalBuild
                    ? "building from source..."
                    : "checking for updates...";
            }
            else if (api.HasStagedUpdate)
            {
                status.Text = $"v{api.CurrentVersion} installed \u00b7 v{api.StagedVersion} staged (applies on next launch)";
            }
            else if (string.IsNullOrEmpty(api.CurrentVersion))
            {
                status.Text = "no hook installed - stage an update to install one";
            }
            else if (string.IsNullOrEmpty(api.LatestVersion))
            {
                status.Text = $"v{api.CurrentVersion} installed";
            }
            else
            {
                status.Text = api.LatestVersion != api.CurrentVersion
                    ? $"v{api.CurrentVersion} installed \u00b7 v{api.LatestVersion} available"
                    : $"v{api.CurrentVersion} installed \u00b7 up to date";
            }

            bool busy = api.Busy;
            checkButton.Enabled.Value = !busy;
            buildButton.Enabled.Value = !busy;
        }

        private void run(UpdateSource source)
        {
            if (api.Busy)
                return;

            _ = Task.Run(() => api.RunAndNotifyAsync(source));
        }
    }
}