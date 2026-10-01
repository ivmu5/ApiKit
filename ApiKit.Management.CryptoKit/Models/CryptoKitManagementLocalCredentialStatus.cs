namespace ApiKit.Management.CryptoKit.Models;

/// <summary>
/// Defines the available states for crypto kit management local credential status.
/// </summary>
public enum CryptoKitManagementLocalCredentialStatus
{
    /// <summary>
    /// Indicates the available credential state.
    /// </summary>
    Available = 0,

    /// <summary>
    /// Indicates the active credential state.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Indicates the retired credential state.
    /// </summary>
    Retired = 2
}
