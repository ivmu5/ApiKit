using ApiKit.Management.Security;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management credential provider.
/// </summary>
public interface IManagementCredentialProvider
{
    /// <summary>
    /// Returns credential ID async.
    /// </summary>
    ValueTask<string> GetCredentialIdAsync(
        ManagementPeerIdentity identity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates challenge response async.
    /// </summary>
    ValueTask<ManagementChallengeResponse> CreateChallengeResponseAsync(
        ManagementPeerIdentity identity,
        ManagementChallenge challenge,
        CancellationToken cancellationToken = default);
}
