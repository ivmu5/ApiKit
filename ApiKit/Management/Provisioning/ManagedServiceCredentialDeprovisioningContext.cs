using ApiKit.Management.Security;

namespace ApiKit.Management.Provisioning;

/// <summary>
/// Defines the management or application behavior of managed service credential deprovisioning context.
/// </summary>
/// <param name="ServiceName">The service name value.</param>
/// <param name="ManagementHostIdentity">The management host identity value.</param>
/// <param name="ServiceRootDirectory">The service root directory value.</param>
public sealed record ManagedServiceCredentialDeprovisioningContext(
    string ServiceName,
    ManagementPeerIdentity ManagementHostIdentity,
    string ServiceRootDirectory);
