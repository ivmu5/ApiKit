using System.Security.Claims;
using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Преобразует подтверждённую identity management peer в <see cref="ClaimsPrincipal"/> для штатной ASP.NET authorization.
/// </summary>
/// <remarks>
/// ACL Named Pipe или права Unix Domain Socket должны применяться до этого уровня. После подключения
/// challenge/response transport также должен передать подтверждённую identity через
/// <see cref="ManagementPeerContext.VerifiedIdentity"/>. Реализация отвечает только за построение principal
/// и выдачу минимально необходимых claims, а не за хранение криптографического key material.
/// </remarks>
public interface IManagementPeerAuthenticator
{
    /// <summary>
    /// Аутентифицирует локального management-клиента и создаёт principal для authorization pipeline.
    /// </summary>
    /// <returns>Аутентифицированный principal или <see langword="null"/> при отказе.</returns>
    ValueTask<ClaimsPrincipal?> AuthenticateAsync(
        ManagementPeerContext peer,
        CancellationToken cancellationToken = default);
}
