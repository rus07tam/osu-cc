using System;
using System.Collections.Generic;
using System.Linq;

namespace osucc.Client
{
    /// <summary>
    /// The <see cref="Plugin.IOsuCcPluginEvents"/> surface backed by <see cref="ClientState"/>.
    /// Each plugin gets its own instance (so subscriptions are per-plugin), but they all observe
    /// the same singleton bootstrap state. <see cref="Clear"/> drops every subscription the
    /// plugin made, so a disabled plugin leaves nothing behind on the shared state.
    /// </summary>
    public sealed class PluginEvents : Plugin.IOsuCcPluginEvents
    {
        private readonly object lockObject = new();
        private readonly HashSet<Action> readySubscriptions = new();
        private readonly HashSet<Action<IReadOnlyList<string>>> faultedSubscriptions = new();

        public event Action? Ready
        {
            add
            {
                if (value == null)
                    return;

                if (ClientState.Status == InitStatus.Ready)
                    value.Invoke();
                else
                {
                    lock (lockObject)
                        readySubscriptions.Add(value);

                    ClientState.Ready += value;
                }
            }
            remove
            {
                if (value == null)
                    return;

                lock (lockObject)
                    readySubscriptions.Remove(value);

                ClientState.Ready -= value;
            }
        }

        public event Action<IReadOnlyList<string>>? InitFailed
        {
            add
            {
                if (value == null)
                    return;

                if (ClientState.IsFaulted)
                    value.Invoke(ClientState.Errors);
                else
                {
                    lock (lockObject)
                        faultedSubscriptions.Add(value);

                    ClientState.Faulted += value;
                }
            }
            remove
            {
                if (value == null)
                    return;

                lock (lockObject)
                    faultedSubscriptions.Remove(value);

                ClientState.Faulted -= value;
            }
        }

        /// <summary>Removes every subscription this instance added to the shared client state.</summary>
        public void Clear()
        {
            Action[] ready;
            Action<IReadOnlyList<string>>[] faulted;

            lock (lockObject)
            {
                ready = readySubscriptions.ToArray();
                faulted = faultedSubscriptions.ToArray();

                readySubscriptions.Clear();
                faultedSubscriptions.Clear();
            }

            foreach (Action handler in ready)
                ClientState.Ready -= handler;

            foreach (Action<IReadOnlyList<string>> handler in faulted)
                ClientState.Faulted -= handler;
        }
    }
}
