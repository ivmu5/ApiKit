using System.Security.Claims;
using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management peer authenticator.
/// </summary>
public interface IManagementPeerAuthenticator
{
    /// <summary>
    /// Authenticates a verified transport peer and creates its principal.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    ValueTask<ClaimsPrincipal?> AuthenticateAsync(
        ManagementPeerContext peer,
        CancellationToken cancellationToken = default);
}
