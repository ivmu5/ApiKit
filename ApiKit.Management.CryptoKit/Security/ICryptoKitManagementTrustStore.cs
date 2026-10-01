using ApiKit.Management.CryptoKit.Models;
using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Security;

/// <summary>
/// Defines the contract for i crypto kit management trust store.
/// </summary>
public interface ICryptoKitManagementTrustStore
{
    /// <summary>
    /// Adds or confirms a peer's trusted public key.
    /// </summary>
    ValueTask TrustAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        ReadOnlyMemory<byte> publicKeySubjectPublicKeyInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks retiring async.
    /// </summary>
    ValueTask MarkRetiringAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        DateTimeOffset acceptUntilUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes async.
    /// </summary>
    ValueTask RevokeAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to retrieve async.
    /// </summary>
    ValueTask<CryptoKitManagementTrustedCredential?> TryGetAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default);
}
