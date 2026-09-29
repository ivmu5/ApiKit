using ApiKit.Management.CryptoKit.Models;

namespace ApiKit.Management.CryptoKit.Security;

/// <summary>
/// Содержит удобные операции bootstrap доверия между management peers.
/// </summary>
public static class CryptoKitManagementTrustStoreExtensions
{
    /// <summary>
    /// Добавляет public credential, экспортированный доверяемым management peer.
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
    /// Отзывает доверие к ранее опубликованному public credential.
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
