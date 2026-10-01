using ApiKit.Management.CryptoKit.Models;
using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Security;

/// <summary>
/// Defines the contract for i crypto kit management public credential provider.
/// </summary>
public interface ICryptoKitManagementPublicCredentialProvider
{
    /// <summary>
    /// Returns active async.
    /// </summary>
    ValueTask<CryptoKitManagementPublicCredential> GetActiveAsync(
        ManagementPeerIdentity identity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns async.
    /// </summary>
    ValueTask<CryptoKitManagementPublicCredential> GetAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all async.
    /// </summary>
    ValueTask<IReadOnlyList<CryptoKitManagementPublicCredential>> GetAllAsync(
        ManagementPeerIdentity identity,
        CancellationToken cancellationToken = default);
}
