namespace osucc.Plugin
{
    /// <summary>
    /// The lifecycle contract every osu!cc plugin implements.
    /// <see cref="Load"/> runs during startup, right after osu.Game.dll loads (before the game instance
    /// exists): register Harmony patches by name, toolbar buttons, settings subsections and config
    /// defaults here. <see cref="AttachToGame"/> runs on the update thread once
    /// <see cref="osucc.Client.ClientApi.Game"/> is available — build drawables and read persisted config here.
    /// </summary>
    public interface IOsuCcPlugin : IDisposable
    {
        /// <summary>Called once during startup with the plugin host.</summary>
        void Load(IOsuCcPluginHost host);

        /// <summary>Called on the update thread when the game instance is ready.</summary>
        void AttachToGame();
    }
}
