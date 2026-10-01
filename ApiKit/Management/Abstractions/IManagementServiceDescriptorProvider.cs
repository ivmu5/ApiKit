using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management service descriptor provider.
/// </summary>
public interface IManagementServiceDescriptorProvider
{
    /// <summary>
    /// Returns descriptor async.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The requested value.</returns>
    ValueTask<ManagementServiceDescriptor> GetDescriptorAsync(
        CancellationToken cancellationToken = default);
}
