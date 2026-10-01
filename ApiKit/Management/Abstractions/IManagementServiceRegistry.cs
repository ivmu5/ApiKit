using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management service registry.
/// </summary>
public interface IManagementServiceRegistry : IManagementServiceCatalog
{
    /// <summary>
    /// Creates or refreshes a service registration.
    /// </summary>
    ValueTask UpsertAsync(
        ManagementServiceRegistration registration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes async.
    /// </summary>
    ValueTask<bool> RemoveAsync(
        string instanceId,
        CancellationToken cancellationToken = default);
}
