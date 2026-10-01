using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Options;

/// <summary>
/// Defines configuration settings for crypto kit management security options.
/// </summary>
public sealed class CryptoKitManagementSecurityOptions
{
    private readonly List<CryptoKitManagementCredentialRegistration> _credentials = [];

    /// <summary>
    /// Gets or sets credentials.
    /// </summary>
    public IReadOnlyList<CryptoKitManagementCredentialRegistration> Credentials => _credentials;

    /// <summary>
    /// Gets or sets clock skew.
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Registers credential.
    /// </summary>
    /// <param name="peerId">The peer ID value.</param>
    /// <param name="kind">The kind value.</param>
    /// <param name="keyId">The key identifier.</param>
    /// <param name="credentialId">The credential identifier.</param>
    /// <param name="isActive">Whether to active.</param>
    /// <param name="createIfMissing">The create if missing value.</param>
    /// <returns>The builder or service collection for further configuration.</returns>
    public CryptoKitManagementSecurityOptions AddCredential(
        string peerId,
        ManagementPeerKind kind,
        string keyId,
        string credentialId,
        bool isActive = true,
        bool createIfMissing = false)
    {
        _credentials.Add(new CryptoKitManagementCredentialRegistration
        {
            PeerId = peerId,
            Kind = kind,
            KeyId = keyId,
            CredentialId = credentialId,
            IsActive = isActive,
            CreateIfMissing = createIfMissing
        });

        return this;
    }
}
