namespace osucc.Client
{
    /// <summary>
    /// The <see cref="Plugin.IOsuCcPluginEvents"/> surface backed by <see cref="ClientState"/>.
    /// Each plugin gets its own instance (so subscriptions are per-plugin), but they all observe
    /// the same singleton bootstrap state.
    /// </summary>
    public sealed class PluginEvents : Plugin.IOsuCcPluginEvents
    {
        public event Action? Ready
        {
            add
            {
                if (ClientState.Status == InitStatus.Ready)
                    value?.Invoke();
                else
                    ClientState.Ready += value;
            }
            remove => ClientState.Ready -= value;
        }

        public event Action<IReadOnlyList<string>>? InitFailed
        {
            add
            {
                if (ClientState.IsFaulted)
                    value?.Invoke(ClientState.Errors);
                else
                    ClientState.Faulted += value;
            }
            remove => ClientState.Faulted -= value;
        }
    }
}