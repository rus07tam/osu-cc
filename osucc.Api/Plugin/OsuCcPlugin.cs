using osu.Framework.Graphics.Sprites;
using osucc.Core;
using System;
using System.Collections.Generic;

namespace osucc.Plugin
{
    public abstract class OsuCcPlugin : IDisposable
    {
        public IOsuCcPluginHost Host { get; private set; } = null!;

        public void Load(IOsuCcPluginHost host)
        {
            Host = host;
            OnLoad();
        }

        /// <summary>
        /// Applies every declarative patch in this plugin's assembly (see
        /// <see cref="OsuCcPatchAttribute"/> / <see cref="OsuCcConstructorPatchAttribute"/>)
        /// through the scoped host. Handles are tracked by the host and reverted on live disable,
        /// so no manual bookkeeping is needed. Call from <see cref="OnLoad"/>. Returns the number
        /// of targets that were actually patched; failures are logged and skipped.
        /// </summary>
        protected int InstallPatches() => OsuCcPatches.Install(Host, GetType().Assembly);

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
