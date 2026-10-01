using ApiKit.Management.Provisioning;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i managed service credential provisioner.
/// </summary>
public interface IManagedServiceCredentialProvisioner
{
    /// <summary>
    /// Provisions async.
    /// </summary>
    ValueTask ProvisionAsync(
        ManagedServiceCredentialProvisioningContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deprovisions async.
    /// </summary>
    ValueTask DeprovisionAsync(
        ManagedServiceCredentialDeprovisioningContext context,
        CancellationToken cancellationToken = default);
}
