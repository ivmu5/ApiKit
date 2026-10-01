using ApiKit.Management.CryptoKit.Models;
using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Security;

/// <summary>
/// Defines the contract for i crypto kit management credential rotation manager.
/// </summary>
public interface ICryptoKitManagementCredentialRotationManager
{
    /// <summary>
    /// Returns credentials async.
    /// </summary>
    ValueTask<IReadOnlyList<CryptoKitManagementCredentialInfo>> GetCredentialsAsync(
        ManagementPeerIdentity identity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prepares an additional credential without changing the active signer.
    /// </summary>
    ValueTask<CryptoKitManagementPublicCredential> PrepareAsync(
        ManagementPeerIdentity identity,
        string keyId,
        string credentialId,
        bool createIfMissing = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates the specified credential and deactivates the previous signer.
    /// </summary>
    ValueTask ActivateAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retires a previously deactivated credential.
    /// </summary>
    ValueTask RetireAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default);
}
