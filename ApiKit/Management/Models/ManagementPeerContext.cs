using ApiKit.Management.Security;

namespace ApiKit.Management.Models;

/// <summary>
/// Carries the transport, operating-system identity, and cryptographically verified peer identity.
/// </summary>
public sealed record ManagementPeerContext
{
    /// <summary>
    /// Gets or sets transport.
    /// </summary>
    public required string Transport { get; init; }

    /// <summary>
    /// Gets or sets operating system identity.
    /// </summary>
    public string? OperatingSystemIdentity { get; init; }

    /// <summary>
    /// Gets or sets verified identity.
    /// </summary>
    public ManagementPeerIdentity? VerifiedIdentity { get; init; }

    /// <summary>
    /// Gets or sets verified credential id.
    /// </summary>
    public string? VerifiedCredentialId { get; init; }

    /// <summary>
    /// Gets or sets properties.
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties { get; init; }
        = new Dictionary<string, string>();
}
