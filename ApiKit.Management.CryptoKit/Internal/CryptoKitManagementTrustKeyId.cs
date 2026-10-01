using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Internal;

internal static class CryptoKitManagementTrustKeyId
{
    private static readonly Encoding StrictUtf8 =
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private const string Prefix = "apikit.management.trust.v3.";
    private const byte FormatVersion = 1;

    internal static string Create(
        ManagementPeerIdentity identity,
        string credentialId)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.PeerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        AppendByte(hash, FormatVersion);
        AppendInt32(hash, (int)identity.Kind);
        AppendString(hash, identity.PeerId);
        AppendString(hash, credentialId);

        return Prefix + Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void AppendByte(
        IncrementalHash hash,
        byte value)
    {
        Span<byte> buffer = stackalloc byte[1];
        buffer[0] = value;
        hash.AppendData(buffer);
    }

    private static void AppendInt32(
        IncrementalHash hash,
        int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        hash.AppendData(buffer);
    }

    private static void AppendString(
        IncrementalHash hash,
        string value)
    {
        var byteCount = StrictUtf8.GetByteCount(value);
        AppendInt32(hash, byteCount);

        if (byteCount == 0)
        {
            return;
        }

        var buffer = new byte[byteCount];

        try
        {
            StrictUtf8.GetBytes(value.AsSpan(), buffer.AsSpan());
            hash.AppendData(buffer);
        }
        finally
        {
            Array.Clear(buffer, 0, buffer.Length);
        }
    }
}
