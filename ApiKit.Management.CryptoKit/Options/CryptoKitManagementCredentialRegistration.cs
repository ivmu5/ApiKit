using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Options;

/// <summary>
/// Registers a local management identity and its cryptographic key identifier.
/// </summary>
public sealed class CryptoKitManagementCredentialRegistration
{
    /// <summary>
    /// Gets or sets peer id.
    /// </summary>
    public required string PeerId { get; init; }

    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public required ManagementPeerKind Kind { get; init; }

    /// <summary>
    /// Gets or sets key id.
    /// </summary>
    public required string KeyId { get; init; }

    /// <summary>
    /// Gets or sets credential id.
    /// </summary>
    public required string CredentialId { get; init; }

    /// <summary>
    /// Gets or sets whether is active is enabled.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Gets or sets whether create if missing is enabled.
    /// </summary>
    public bool CreateIfMissing { get; init; }
}
