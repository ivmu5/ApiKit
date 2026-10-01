using ApiKit.Management.Models;
using ApiKit.Management.Security;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management peer verifier.
/// </summary>
public interface IManagementPeerVerifier
{
    /// <summary>
    /// Verifies async.
    /// </summary>
    ValueTask<ManagementAuthenticationResult> VerifyAsync(
        ManagementPeerContext transportPeer,
        ManagementChallenge challenge,
        ManagementChallengeResponse response,
        CancellationToken cancellationToken = default);
}
