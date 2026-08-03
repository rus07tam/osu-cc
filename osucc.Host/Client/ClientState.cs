namespace osucc.Client
{
    public enum InitStatus
    {
        Pending,
        Ready,
        Failed
    }

    /// <summary>Collects the bootstrap result — whether every patch attached and any errors — which drives the startup notification.</summary>
    public static class ClientState
    {
        private static readonly object lockObject = new();

        private static readonly List<string> errors = new();

        /// <summary>Fires once the client finished startup and is ready (see <see cref="MarkReady"/>).</summary>
        public static event Action? Ready;

        /// <summary>Fires when the client fails to initialise, carrying the collected errors.</summary>
        public static event Action<IReadOnlyList<string>>? Faulted;

        public static InitStatus Status { get; private set; } = InitStatus.Pending;

        public static IReadOnlyList<string> Errors
        {
            get
            {
                lock (lockObject)
                    return errors.ToArray();
            }
        }

        public static bool IsFaulted => Status == InitStatus.Failed;

        public static void RecordPatchResult(string name, bool ok)
        {
            bool transitionedToFailed = false;

            lock (lockObject)
            {
                if (!ok)
                {
                    errors.Add($"patch '{name}' failed");
                    transitionedToFailed = Status != InitStatus.Failed;
                    Status = InitStatus.Failed;
                }
            }

            if (transitionedToFailed)
                Faulted?.Invoke(errors.ToArray());
        }

        public static void AddError(string message)
        {
            bool transitionedToFailed = false;

            lock (lockObject)
            {
                errors.Add(message);
                transitionedToFailed = Status != InitStatus.Failed;
                Status = InitStatus.Failed;
            }

            if (transitionedToFailed)
                Faulted?.Invoke(errors.ToArray());
        }

        public static void MarkReady()
        {
            lock (lockObject)
                Status = InitStatus.Ready;

            Ready?.Invoke();
        }
    }
}
