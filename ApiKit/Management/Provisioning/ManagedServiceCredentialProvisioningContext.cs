using ApiKit.Management.Lifecycle;
using ApiKit.Management.Security;

namespace ApiKit.Management.Provisioning;

/// <summary>
/// Defines the management or application behavior of managed service credential provisioning context.
/// </summary>
/// <param name="Manifest">The manifest value.</param>
/// <param name="ManagementHostIdentity">The management host identity value.</param>
/// <param name="ServiceRootDirectory">The service root directory value.</param>
/// <param name="InstallDirectory">The install directory value.</param>
public sealed record ManagedServiceCredentialProvisioningContext(
    ManagedServicePackageManifest Manifest,
    ManagementPeerIdentity ManagementHostIdentity,
    string ServiceRootDirectory,
    string InstallDirectory);
