using System.Buffers.Binary;
using System.Text;

namespace ApiKit.Management.Security;

/// <summary>
/// Builds the canonical, domain-separated binary representation that is signed during challenge-response authentication.
/// </summary>
/// <remarks>
/// The format binds both peer identities, nonce, transport, purpose, protocol version, and timestamps. Identity metadata is descriptive only and is deliberately excluded from signatures.
/// </remarks>
public static class ManagementChallengeProofPayload
{
    private static readonly Encoding StrictUtf8 =
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private static readonly byte[] DomainSeparator =
        "ApiKit.Management.ChallengeProof"u8.ToArray();

    private const byte FormatVersion = 1;

    /// <summary>
    /// Serializes the complete challenge-response context into canonical bytes for signing or verification.
    /// </summary>
    /// <param name="challenge">The challenge to process.</param>
    /// <param name="responder">The response issuer identity.</param>
    /// <param name="credentialId">The credential identifier.</param>
    /// <param name="createdAtUtc">The UTC timestamp when the response was created.</param>
    /// <returns>The created value.</returns>
    public static byte[] Create(
        ManagementChallenge challenge,
        ManagementPeerIdentity responder,
        string credentialId,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(responder);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);

        ValidateChallenge(challenge);
        ValidateIdentity(responder, nameof(responder));

        using var stream = new MemoryStream(capacity: 512);

        WriteBytes(stream, DomainSeparator);
        stream.WriteByte(FormatVersion);

        WriteString(stream, challenge.ChallengeId);
        WriteIdentity(stream, challenge.Issuer);
        WriteIdentity(stream, challenge.Subject);
        WriteBytes(stream, challenge.Nonce);
        WriteString(stream, challenge.Transport);
        WriteString(stream, challenge.Purpose);
        WriteInt32(stream, challenge.ProtocolVersion);
        WriteInt64(stream, challenge.IssuedAtUtc.ToUnixTimeMilliseconds());
        WriteInt64(stream, challenge.ExpiresAtUtc.ToUnixTimeMilliseconds());

        WriteIdentity(stream, responder);
        WriteString(stream, credentialId);
        WriteInt64(stream, createdAtUtc.ToUnixTimeMilliseconds());

        return stream.ToArray();
    }

    private static void ValidateChallenge(ManagementChallenge challenge)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(challenge.ChallengeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(challenge.Transport);
        ArgumentException.ThrowIfNullOrWhiteSpace(challenge.Purpose);

        ValidateIdentity(challenge.Issuer, nameof(challenge.Issuer));
        ValidateIdentity(challenge.Subject, nameof(challenge.Subject));

        if (challenge.Nonce is null || challenge.Nonce.Length == 0)
        {
            throw new ArgumentException(
                "Management challenge должен содержать непустой nonce.",
                nameof(challenge));
        }

        if (challenge.ProtocolVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(challenge),
                "Версия management protocol должна быть положительной.");
        }

        if (challenge.ExpiresAtUtc <= challenge.IssuedAtUtc)
        {
            throw new ArgumentException(
                "Время истечения management challenge должно быть позже времени его выпуска.",
                nameof(challenge));
        }
    }

    private static void ValidateIdentity(
        ManagementPeerIdentity identity,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(identity, parameterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.PeerId, parameterName);

        if (identity.Kind == ManagementPeerKind.Unknown ||
            !Enum.IsDefined(identity.Kind))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Неизвестный тип management peer.");
        }
    }

    private static void WriteIdentity(
        Stream stream,
        ManagementPeerIdentity identity)
    {
        ValidateIdentity(identity, nameof(identity));

        WriteString(stream, identity.PeerId);
        WriteInt32(stream, (int)identity.Kind);
        WriteNullableString(stream, identity.InstanceId);
    }

    private static void WriteString(Stream stream, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var byteCount = StrictUtf8.GetByteCount(value);
        WriteInt32(stream, byteCount);

        if (byteCount == 0)
        {
            return;
        }

        var bytes = StrictUtf8.GetBytes(value);
        stream.Write(bytes);
    }

    private static void WriteNullableString(Stream stream, string? value)
    {
        if (value is null)
        {
            stream.WriteByte(0);
            return;
        }

        stream.WriteByte(1);
        WriteString(stream, value);
    }

    private static void WriteBytes(Stream stream, ReadOnlySpan<byte> value)
    {
        WriteInt32(stream, value.Length);
        stream.Write(value);
    }

    private static void WriteInt32(Stream stream, int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteInt64(Stream stream, long value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(buffer, value);
        stream.Write(buffer);
    }
}
