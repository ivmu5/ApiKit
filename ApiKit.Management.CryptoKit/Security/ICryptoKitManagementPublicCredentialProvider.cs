using ApiKit.Management.CryptoKit.Models;
using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Security;

/// <summary>
/// Предоставляет публичную часть локального management credential для безопасного bootstrap доверия.
/// </summary>
public interface ICryptoKitManagementPublicCredentialProvider
{
    /// <summary>
    /// Возвращает активный public credential указанной identity.
    /// Закрытый ключ через этот контракт никогда не публикуется.
    /// </summary>
    ValueTask<CryptoKitManagementPublicCredential> GetActiveAsync(
        ManagementPeerIdentity identity,
        CancellationToken cancellationToken = default);
}
