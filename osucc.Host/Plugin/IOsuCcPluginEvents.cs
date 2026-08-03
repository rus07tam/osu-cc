namespace osucc.Plugin
{
    /// <summary>
    /// Client lifecycle events a plugin can observe. Subscribing after the event already fired
    /// invokes the handler immediately with the current state, so plugins can subscribe during
    /// <see cref="IOsuCcPlugin.Load"/> without missing startup.
    /// </summary>
    public interface IOsuCcPluginEvents
    {
        /// <summary>Fires once the client finished startup and is ready. Already-ready clients fire immediately on subscribe.</summary>
        event Action? Ready;

        /// <summary>Fires when the client failed to initialise, carrying the collected error messages. Already-failed clients fire immediately on subscribe.</summary>
        event Action<IReadOnlyList<string>>? InitFailed;
    }
}