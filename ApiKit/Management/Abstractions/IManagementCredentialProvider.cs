using ApiKit.Management.Security;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Предоставляет доступ к management credential без раскрытия закрытого key material вызывающему коду.
/// </summary>
/// <remarks>
/// Реализация может использовать CryptoKit, аппаратное хранилище или иной backend. ApiKit должен получать
/// только идентификатор credential и proof владения им, но не приватный ключ или общий секрет.
/// </remarks>
public interface IManagementCredentialProvider
{
    /// <summary>
    /// Возвращает идентификатор credential, которым текущий участник будет подтверждать указанную identity.
    /// </summary>
    ValueTask<string> GetCredentialIdAsync(
        ManagementPeerIdentity identity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Формирует ответ на challenge, доказывающий владение credential указанной identity.
    /// </summary>
    ValueTask<ManagementChallengeResponse> CreateChallengeResponseAsync(
        ManagementPeerIdentity identity,
        ManagementChallenge challenge,
        CancellationToken cancellationToken = default);
}
