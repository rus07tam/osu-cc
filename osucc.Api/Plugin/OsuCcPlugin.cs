using osu.Framework.Graphics.Sprites;
using osucc.Core;
using System;
using System.Collections.Generic;

namespace osucc.Plugin
{
    public abstract class OsuCcPlugin : IDisposable
    {
        public IOsuCcPluginHost Host { get; private set; } = null!;

        /// <summary>Whether the plugin is currently enabled.</summary>
        public bool Enabled => Host?.Enabled ?? false;

        /// <summary>
        /// Ordered list of patch instances created by this plugin.
        /// Override in subclasses to declare plugin patches.
        /// </summary>
        public virtual IReadOnlyList<OsuCcPatch> Patches => Array.Empty<OsuCcPatch>();

        public void Load(IOsuCcPluginHost host)
        {
            Host = host;
            OnLoad();
        }

        /// <summary>
        /// Installs all patches declared in <see cref="Patches"/> through the scoped host.
        /// Returns the count of successfully installed patches.
        /// </summary>
        protected int InstallPatches()
        {
            int count = 0;
            foreach (var patch in Patches)
            {
                if (Host.AddPatch(patch))
                    count++;
            }
            return count;
        }

        protected abstract void OnLoad();

        public virtual void AttachToGame() { }

        public virtual IconUsage? Icon => null;

        public virtual void OnInstall() { }

        public virtual void OnUninstall() { }

        public virtual void OnUpdate(string previousVersion) { }

        public virtual int SchemaVersion => 0;

        public virtual IEnumerable<IPluginMigration> Migrations => Array.Empty<IPluginMigration>();

        public virtual void Dispose() => GC.SuppressFinalize(this);
    }
}
