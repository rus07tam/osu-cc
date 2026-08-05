namespace OsuCcUpdater
{
    /// <summary>Where the updater plugin pulls a new osu-cc build from.</summary>
    public enum UpdateSource
    {
        /// <summary>Downloads the prebuilt <c>osucc-runtime-&lt;version&gt;.zip</c> from the latest GitHub release.</summary>
        GithubBundle,

        /// <summary>Clones the official repo and builds the runtime bundle locally with the .NET SDK.</summary>
        LocalBuild,
    }

    public enum UpdateOutcome
    {
        /// <summary>The installed hook is already at or above the newest available version.</summary>
        UpToDate,

        /// <summary>A new build was downloaded/built and staged for the next launch.</summary>
        Staged,

        /// <summary>A staged update from an earlier run is already waiting; nothing changed.</summary>
        AlreadyStaged,

        /// <summary>The operation failed; <see cref="UpdateResult.Message"/> explains why.</summary>
        Failed,

        /// <summary>The operation was cancelled.</summary>
        Cancelled,
    }

    /// <summary>Result of an update attempt.</summary>
    public sealed record UpdateResult(UpdateOutcome Outcome, string? Version, string? Message)
    {
        public static UpdateResult Of(UpdateOutcome outcome, string? version = null, string? message = null)
            => new(outcome, version, message);
    }
}