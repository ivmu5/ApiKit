using ApiKit.Management.Lifecycle;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i managed service lifecycle controller.
/// </summary>
public interface IManagedServiceLifecycleController
{
    /// <summary>
    /// Returns state async.
    /// </summary>
    ValueTask<ManagedServiceState> GetStateAsync(
        string serviceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts async.
    /// </summary>
    ValueTask StartAsync(
        string serviceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops async.
    /// </summary>
    ValueTask StopAsync(
        string serviceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts the specified managed service.
    /// </summary>
    ValueTask RestartAsync(
        string serviceName,
        CancellationToken cancellationToken = default);
}
