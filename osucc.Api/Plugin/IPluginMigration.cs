using System;

namespace osucc.Plugin
{
    public interface IPluginMigration
    {
        int ToVersion { get; }
        void Apply(PluginSettings settings, Action<string> log);
    }
}
