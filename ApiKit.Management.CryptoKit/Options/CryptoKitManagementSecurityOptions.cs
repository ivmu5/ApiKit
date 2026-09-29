using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Options;

/// <summary>
/// Настройки интеграции management security с CryptoKit.
/// </summary>
public sealed class CryptoKitManagementSecurityOptions
{
    private readonly List<CryptoKitManagementCredentialRegistration> _credentials = [];

    /// <summary>
    /// Зарегистрированные локальные credentials. Коллекция предназначена только для чтения;
    /// добавление выполняется через <see cref="AddCredential"/>.
    /// </summary>
    public IReadOnlyList<CryptoKitManagementCredentialRegistration> Credentials => _credentials;

    /// <summary>
    /// Допустимое расхождение локальных часов при проверке временных границ challenge response.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Добавляет RSA credential для management identity.
    /// </summary>
    /// <param name="peerId">Стабильный идентификатор peer.</param>
    /// <param name="kind">Тип peer.</param>
    /// <param name="keyId">Логический идентификатор RSA-ключа в CryptoKit.</param>
    /// <param name="credentialId">Публичный идентификатор credential.</param>
    /// <param name="isActive">Использовать credential для формирования новых proof.</param>
    /// <param name="createIfMissing">Разрешить создание отсутствующего RSA-ключа.</param>
    /// <returns>Текущий объект настроек.</returns>
    public CryptoKitManagementSecurityOptions AddCredential(
        string peerId,
        ManagementPeerKind kind,
        string keyId,
        string credentialId,
        bool isActive = true,
        bool createIfMissing = false)
    {
        _credentials.Add(new CryptoKitManagementCredentialRegistration
        {
            PeerId = peerId,
            Kind = kind,
            KeyId = keyId,
            CredentialId = credentialId,
            IsActive = isActive,
            CreateIfMissing = createIfMissing
        });

        return this;
    }
}
