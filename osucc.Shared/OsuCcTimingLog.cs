namespace osucc.Common;

/// <summary>
/// Minimal host-agnostic logging for the shared library. Each surface (launcher console, in-game
/// timing log) plugs in its own sink; callers that never set one simply swallow the messages.
/// </summary>
public static class OsuCcTimingLog
{
    /// <summary>Sink invoked for error messages; <c>null</c> means "no-op".</summary>
    public static Action<string>? Error { get; set; }

    /// <summary>Reports an error line through the configured sink.</summary>
    public static void ReportError(string message) => Error?.Invoke(message);
}
