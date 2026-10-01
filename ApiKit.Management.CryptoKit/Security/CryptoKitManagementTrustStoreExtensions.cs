using ApiKit.Management.CryptoKit.Models;

namespace ApiKit.Management.CryptoKit.Security;

/// <summary>
/// Defines extension methods for crypto kit management trust store extensions.
/// </summary>
public static class CryptoKitManagementTrustStoreExtensions
{
    /// <summary>
    /// Adds or confirms a peer's trusted public key.
    /// </summary>
    public static ValueTask TrustAsync(
        this ICryptoKitManagementTrustStore trustStore,
        CryptoKitManagementPublicCredential credential,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trustStore);
        ArgumentNullException.ThrowIfNull(credential);

        return trustStore.TrustAsync(
            credential.Identity,
            credential.CredentialId,
            credential.PublicKeySubjectPublicKeyInfo,
            cancellationToken);
    }

    /// <summary>
    /// Marks retiring async.
    /// </summary>
    public static ValueTask MarkRetiringAsync(
        this ICryptoKitManagementTrustStore trustStore,
        CryptoKitManagementPublicCredential credential,
        DateTimeOffset acceptUntilUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trustStore);
        ArgumentNullException.ThrowIfNull(credential);

        return trustStore.MarkRetiringAsync(
            credential.Identity,
            credential.CredentialId,
            acceptUntilUtc,
            cancellationToken);
    }

    /// <summary>
    /// Revokes async.
    /// </summary>
    public static ValueTask RevokeAsync(
        this ICryptoKitManagementTrustStore trustStore,
        CryptoKitManagementPublicCredential credential,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trustStore);
        ArgumentNullException.ThrowIfNull(credential);

        return trustStore.RevokeAsync(
            credential.Identity,
            credential.CredentialId,
            cancellationToken);
    }
}
