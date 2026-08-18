using osu.Framework.Graphics.Sprites;
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
