using ApiKit.Management.Lifecycle;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i managed service installer.
/// </summary>
public interface IManagedServiceInstaller
{
    /// <summary>
    /// Installs the verified managed service package and provisions its credentials.
    /// </summary>
    ValueTask InstallAsync(
        ManagedServicePackageManifest manifest,
        string packageDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates async.
    /// </summary>
    ValueTask UpdateAsync(
        ManagedServicePackageManifest manifest,
        string packageDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstalls a managed service and revokes its provisioned credentials.
    /// </summary>
    ValueTask UninstallAsync(
        string serviceName,
        CancellationToken cancellationToken = default);
}
