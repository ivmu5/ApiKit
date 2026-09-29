using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Security;

/// <summary>
/// Управляет доверенными открытыми RSA-ключами management peer в хранилище CryptoKit.
/// </summary>
public interface ICryptoKitManagementTrustStore
{
    /// <summary>
    /// Добавляет trusted public key для указанной identity и credential.
    /// Повторное добавление того же ключа идемпотентно; подмена ключа при том же credential id запрещена.
    /// </summary>
    ValueTask TrustAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        ReadOnlyMemory<byte> publicKeySubjectPublicKeyInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет trusted public key. Операция идемпотентна.
    /// </summary>
    ValueTask RevokeAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Пытается получить trusted public key в формате SubjectPublicKeyInfo.
    /// </summary>
    ValueTask<byte[]?> TryGetPublicKeyAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default);
}
