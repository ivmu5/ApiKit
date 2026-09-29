using ApiKit.Management.Models;
using ApiKit.Management.Security;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Проверяет криптографическое доказательство identity удалённого участника management plane.
/// </summary>
/// <remarks>
/// Проверка credential дополняет, а не заменяет ACL/permissions transport-уровня. Реализация должна сверять
/// responder identity с ожидаемым subject challenge и не доверять identity, переданной самим клиентом без proof.
/// </remarks>
public interface IManagementPeerVerifier
{
    /// <summary>
    /// Проверяет ответ peer на ранее выпущенный challenge.
    /// </summary>
    ValueTask<ManagementAuthenticationResult> VerifyAsync(
        ManagementPeerContext transportPeer,
        ManagementChallenge challenge,
        ManagementChallengeResponse response,
        CancellationToken cancellationToken = default);
}
