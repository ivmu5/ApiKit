using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management service catalog.
/// </summary>
public interface IManagementServiceCatalog
{
    /// <summary>
    /// Returns services async.
    /// </summary>
    ValueTask<IReadOnlyList<RegisteredManagementService>> GetServicesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns services async.
    /// </summary>
    ValueTask<IReadOnlyList<RegisteredManagementService>> GetServicesAsync(
        string serviceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds async.
    /// </summary>
    ValueTask<RegisteredManagementService?> FindAsync(
        string instanceId,
        CancellationToken cancellationToken = default);
}
