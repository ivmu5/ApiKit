using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Models;

/// <summary>
/// Defines the management or application behavior of crypto kit management trusted credential.
/// </summary>
/// <param name="Identity">The identity value.</param>
/// <param name="CredentialId">The credential ID value.</param>
/// <param name="PublicKeySubjectPublicKeyInfo">The public key subject public key info value.</param>
/// <param name="Status">The status value.</param>
/// <param name="TrustedAtUtc">The trusted at UTC value.</param>
/// <param name="AcceptUntilUtc">The accept until UTC value.</param>
public sealed record CryptoKitManagementTrustedCredential(
    ManagementPeerIdentity Identity,
    string CredentialId,
    byte[] PublicKeySubjectPublicKeyInfo,
    CryptoKitManagementTrustedCredentialStatus Status,
    DateTimeOffset TrustedAtUtc,
    DateTimeOffset? AcceptUntilUtc);
