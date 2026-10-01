namespace ApiKit.Management.CryptoKit.Options;

/// <summary>
/// Defines configuration settings for crypto kit managed service provisioning options.
/// </summary>
public sealed class CryptoKitManagedServiceProvisioningOptions
{
    /// <summary>
    /// Gets or sets service RSA key size.
    /// </summary>
    public int ServiceRsaKeySize { get; set; } = 3072;

    /// <summary>
    /// Gets or sets crypto kit directory name.
    /// </summary>
    public string CryptoKitDirectoryName { get; set; } = "cryptokit";
}
