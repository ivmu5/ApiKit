using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Models;

/// <summary>
/// Defines the management or application behavior of crypto kit management credential info.
/// </summary>
/// <param name="Identity">The identity value.</param>
/// <param name="KeyId">The key ID value.</param>
/// <param name="CredentialId">The credential ID value.</param>
/// <param name="Status">The status value.</param>
/// <param name="CreateIfMissing">The create if missing value.</param>
public sealed record CryptoKitManagementCredentialInfo(
    ManagementPeerIdentity Identity,
    string KeyId,
    string CredentialId,
    CryptoKitManagementLocalCredentialStatus Status,
    bool CreateIfMissing);
