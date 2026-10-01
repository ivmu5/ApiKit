namespace ApiKit.Management.Security;

/// <summary>
/// Represents a one-time challenge bound to issuer, subject, nonce, protocol context, and expiration.
/// </summary>
public sealed record ManagementChallenge
{
    /// <summary>
    /// Gets or sets challenge id.
    /// </summary>
    public required string ChallengeId { get; init; }

    /// <summary>
    /// Gets or sets issuer.
    /// </summary>
    public required ManagementPeerIdentity Issuer { get; init; }

    /// <summary>
    /// Gets or sets subject.
    /// </summary>
    public required ManagementPeerIdentity Subject { get; init; }

    /// <summary>
    /// Gets or sets nonce.
    /// </summary>
    public required byte[] Nonce { get; init; }

    /// <summary>
    /// Gets or sets transport.
    /// </summary>
    public required string Transport { get; init; }

    /// <summary>
    /// Gets or sets purpose.
    /// </summary>
    public required string Purpose { get; init; }

    /// <summary>
    /// Gets or sets protocol version.
    /// </summary>
    public required int ProtocolVersion { get; init; }

    /// <summary>
    /// Gets or sets issued at utc.
    /// </summary>
    public required DateTimeOffset IssuedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets expires at utc.
    /// </summary>
    public required DateTimeOffset ExpiresAtUtc { get; init; }
}
