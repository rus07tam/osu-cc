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
            lock (lockObject)
            {
                if (!ok)
                {
                    errors.Add($"patch '{name}' failed");
                    Status = InitStatus.Failed;
                }
            }
        }

        public static void AddError(string message)
        {
            lock (lockObject)
            {
                errors.Add(message);
                Status = InitStatus.Failed;
            }
        }

        public static void MarkReady() => Status = InitStatus.Ready;
    }
}
