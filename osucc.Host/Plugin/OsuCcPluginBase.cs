using System;
using System.Collections.Generic;

namespace osucc.Plugin
{
    /// <summary>
    /// Convenience base class for plugins: stores the <see cref="Host"/> during
    /// <see cref="Load"/> (so <see cref="OnLoad"/> can use it right away), adds empty lifecycle
    /// hooks and migration defaults. Implementations override only what they need.
    /// </summary>
    public abstract class OsuCcPluginBase : IOsuCcPlugin, IPluginLifecycle, IPluginMigrations
    {
        /// <summary>The host bound to this plugin, available from <see cref="OnLoad"/> onwards.</summary>
        protected IOsuCcPluginHost Host { get; private set; } = null!;

        public void Load(IOsuCcPluginHost host)
        {
            Host = host;
            OnLoad();
        }

        /// <summary>Registers toolbar buttons, settings subsections, config defaults and Harmony patches here.</summary>
        protected abstract void OnLoad();

        public virtual void AttachToGame()
        {
        }

        public virtual void OnInstall(IOsuCcPluginHost host)
        {
        }

        public virtual void OnUninstall(IOsuCcPluginHost host)
        {
        }

        public virtual void OnUpdate(IOsuCcPluginHost host, string previousVersion)
        {
        }

        public virtual int SchemaVersion => 0;

        public virtual IEnumerable<IPluginMigration> Migrations => Array.Empty<IPluginMigration>();

        public virtual void Dispose() => GC.SuppressFinalize(this);
    }
}
