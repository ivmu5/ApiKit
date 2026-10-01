namespace ApiKit.Management.Security;

/// <summary>
/// Defines a result of management authentication result.
/// </summary>
public sealed record ManagementAuthenticationResult
{
    /// <summary>
    /// Gets or sets whether succeeded is enabled.
    /// </summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// Gets or sets identity.
    /// </summary>
    public ManagementPeerIdentity? Identity { get; init; }

    /// <summary>
    /// Gets or sets credential id.
    /// </summary>
    public string? CredentialId { get; init; }

    /// <summary>
    /// Gets or sets failure code.
    /// </summary>
    public string? FailureCode { get; init; }

    /// <summary>
    /// Gets or sets failure reason.
    /// </summary>
    public string? FailureReason { get; init; }
}
