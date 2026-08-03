namespace osucc.Client
{
    /// <summary>Severity of a notification posted through <see cref="ClientNotifications"/> or <see cref="Plugin.IOsuCcPluginHost.Notify"/>.</summary>
    public enum NotificationKind
    {
        Success,
        Error,
        Warning,
        Info
    }
}
