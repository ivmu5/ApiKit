namespace ApiKit.Management.Security;

/// <summary>
/// Contains the credential identifier, signed proof, and authenticated responder identity.
/// </summary>
public sealed record ManagementChallengeResponse
{
    /// <summary>
    /// Gets or sets challenge id.
    /// </summary>
    public required string ChallengeId { get; init; }

    /// <summary>
    /// Gets or sets responder.
    /// </summary>
    public required ManagementPeerIdentity Responder { get; init; }

    /// <summary>
    /// Gets or sets credential id.
    /// </summary>
    public required string CredentialId { get; init; }

    /// <summary>
    /// Gets or sets proof.
    /// </summary>
    public required byte[] Proof { get; init; }

    /// <summary>
    /// Gets or sets created at utc.
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }
}
