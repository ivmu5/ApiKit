using ApiKit.Management.Security;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management challenge provider.
/// </summary>
public interface IManagementChallengeProvider
{
    /// <summary>
    /// Creates challenge async.
    /// </summary>
    ValueTask<ManagementChallenge> CreateChallengeAsync(
        ManagementPeerIdentity issuer,
        ManagementPeerIdentity subject,
        string transport,
        string purpose,
        int protocolVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically consumes a previously issued challenge; a second attempt returns no challenge.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    ValueTask<ManagementChallenge?> ConsumeChallengeAsync(
        string challengeId,
        CancellationToken cancellationToken = default);
}
