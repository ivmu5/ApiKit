using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Options;

/// <summary>
/// Связывает management identity с RSA-ключом, которым процесс подтверждает владение этой identity.
/// </summary>
public sealed class CryptoKitManagementCredentialRegistration
{
    /// <summary>Стабильный идентификатор management peer.</summary>
    public required string PeerId { get; init; }

    /// <summary>Тип management peer.</summary>
    public required ManagementPeerKind Kind { get; init; }

    /// <summary>Логический идентификатор RSA-ключа в CryptoKit.</summary>
    public required string KeyId { get; init; }

    /// <summary>
    /// Публичный идентификатор credential, передаваемый второй стороне и используемый при выборе trusted public key.
    /// </summary>
    public required string CredentialId { get; init; }

    /// <summary>
    /// Указывает, является ли credential активным для формирования новых proof.
    /// Для одной identity должен существовать ровно один активный credential.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Разрешает CryptoKit создать RSA-ключ, если он отсутствует.
    /// По умолчанию создание ключевого материала через runtime adapter запрещено.
    /// </summary>
    public bool CreateIfMissing { get; init; }
}
