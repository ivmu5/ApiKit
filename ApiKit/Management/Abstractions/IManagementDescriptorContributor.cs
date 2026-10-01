using ApiKit.Management.Builders;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management descriptor contributor.
/// </summary>
public interface IManagementDescriptorContributor
{
    /// <summary>
    /// Adds this contributor's metadata to the published service descriptor.
    /// </summary>
    /// <param name="builder">The service or endpoint builder.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    ValueTask ContributeAsync(
        ManagementServiceDescriptorBuilder builder,
        CancellationToken cancellationToken = default);
}
