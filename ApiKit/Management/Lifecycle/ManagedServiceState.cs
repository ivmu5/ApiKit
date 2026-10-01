namespace ApiKit.Management.Lifecycle;

/// <summary>
/// Defines the available states for managed service state.
/// </summary>
public enum ManagedServiceState
{
    /// <summary>
    /// Indicates that the managed service state is unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// Indicates that the managed service is stopped.
    /// </summary>
    Stopped,

    /// <summary>
    /// Indicates that the managed service is starting.
    /// </summary>
    Starting,

    /// <summary>
    /// Indicates that the managed service is running.
    /// </summary>
    Running,

    /// <summary>
    /// Indicates that the managed service is paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Indicates that the managed service is stopping.
    /// </summary>
    Stopping,

    /// <summary>
    /// Defines the management or application behavior of managed service state.
    /// </summary>
    Failed
}
