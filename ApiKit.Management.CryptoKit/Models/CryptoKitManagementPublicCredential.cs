using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Models;

/// <summary>
/// Описывает публичную часть management credential, которую можно передать доверяющей стороне.
/// </summary>
/// <param name="Identity">Identity владельца credential.</param>
/// <param name="CredentialId">Публичный идентификатор credential.</param>
/// <param name="PublicKeySubjectPublicKeyInfo">Открытый RSA-ключ в формате SubjectPublicKeyInfo.</param>
public sealed record CryptoKitManagementPublicCredential(
    ManagementPeerIdentity Identity,
    string CredentialId,
    byte[] PublicKeySubjectPublicKeyInfo);
