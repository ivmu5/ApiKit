using System.Security.Cryptography;
using System.Text;
using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Internal;

internal static class CryptoKitManagementTrustKeyId
{
    private const string Prefix = "apikit.management.trust.";

    internal static string Create(
        ManagementPeerIdentity identity,
        string credentialId)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.PeerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);

        var source = Encoding.UTF8.GetBytes(
            $"{(int)identity.Kind}\0{identity.PeerId}\0{credentialId}");

        try
        {
            var hash = SHA256.HashData(source);
            return Prefix + Convert.ToHexString(hash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(source);
        }
    }
}
