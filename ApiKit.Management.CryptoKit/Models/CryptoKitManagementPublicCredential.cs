using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Models;

/// <summary>
/// Defines the management or application behavior of crypto kit management public credential.
/// </summary>
/// <param name="Identity">The identity value.</param>
/// <param name="CredentialId">The credential ID value.</param>
/// <param name="PublicKeySubjectPublicKeyInfo">The public key subject public key info value.</param>
/// <param name="Status">The status value.</param>
public sealed record CryptoKitManagementPublicCredential(
    ManagementPeerIdentity Identity,
    string CredentialId,
    byte[] PublicKeySubjectPublicKeyInfo,
    CryptoKitManagementLocalCredentialStatus Status);
