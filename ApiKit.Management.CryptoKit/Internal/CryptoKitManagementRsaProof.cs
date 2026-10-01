using System.Security.Cryptography;

namespace ApiKit.Management.CryptoKit.Internal;

/// <summary>
/// Signs and verifies management protocol proofs using RSA-PSS with SHA-256.
/// </summary>
internal static class CryptoKitManagementRsaProof
{
    private const int MinimumRsaKeySizeBits = 2048;

    internal static byte[] Sign(
        ReadOnlySpan<byte> privateKeyPkcs8,
        ReadOnlySpan<byte> payload)
    {
        if (privateKeyPkcs8.IsEmpty)
        {
            throw new ArgumentException(
                "Закрытый management RSA-ключ не может быть пустым.",
                nameof(privateKeyPkcs8));
        }

        using var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(privateKeyPkcs8, out var bytesRead);

        EnsureFullyConsumed(
            bytesRead,
            privateKeyPkcs8.Length,
            "Закрытый management RSA-ключ содержит лишние или некорректные данные.");

        EnsureMinimumKeySize(rsa.KeySize);

        return rsa.SignData(
            payload,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);
    }

    internal static bool Verify(
        ReadOnlySpan<byte> publicKeySubjectPublicKeyInfo,
        ReadOnlySpan<byte> payload,
        ReadOnlySpan<byte> proof)
    {
        using var rsa = ImportPublicKey(publicKeySubjectPublicKeyInfo);

        return rsa.VerifyData(
            payload,
            proof,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pss);
    }

    internal static void ValidatePublicKey(
        ReadOnlySpan<byte> publicKeySubjectPublicKeyInfo)
    {
        using var _ = ImportPublicKey(publicKeySubjectPublicKeyInfo);
    }

    private static RSA ImportPublicKey(
        ReadOnlySpan<byte> publicKeySubjectPublicKeyInfo)
    {
        if (publicKeySubjectPublicKeyInfo.IsEmpty)
        {
            throw new ArgumentException(
                "Trusted public key не может быть пустым.",
                nameof(publicKeySubjectPublicKeyInfo));
        }

        var rsa = RSA.Create();

        try
        {
            rsa.ImportSubjectPublicKeyInfo(
                publicKeySubjectPublicKeyInfo,
                out var bytesRead);

            EnsureFullyConsumed(
                bytesRead,
                publicKeySubjectPublicKeyInfo.Length,
                "Trusted public key содержит лишние или некорректные данные.");

            EnsureMinimumKeySize(rsa.KeySize);
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    private static void EnsureFullyConsumed(
        int bytesRead,
        int totalLength,
        string message)
    {
        if (bytesRead != totalLength)
        {
            throw new CryptographicException(message);
        }
    }

    private static void EnsureMinimumKeySize(int keySizeBits)
    {
        if (keySizeBits < MinimumRsaKeySizeBits)
        {
            throw new CryptographicException(
                $"Размер management RSA-ключа должен быть не меньше {MinimumRsaKeySizeBits} бит.");
        }
    }
}
