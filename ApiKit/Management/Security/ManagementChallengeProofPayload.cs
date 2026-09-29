using System.Buffers.Binary;
using System.Text;

namespace ApiKit.Management.Security;

/// <summary>
/// Формирует каноническое бинарное представление management challenge,
/// которое должно использоваться при создании и проверке криптографического proof.
/// </summary>
/// <remarks>
/// <para>
/// Формат намеренно не зависит от JSON-настроек transport. Все строки кодируются UTF-8
/// с 32-битным big-endian префиксом длины, числовые значения — в big-endian.
/// </para>
/// <para>
/// В proof включается security identity и protocol context, но не произвольная
/// <see cref="ManagementPeerIdentity.Metadata"/>, поскольку metadata не является
/// самостоятельным источником доверия.
/// </para>
/// </remarks>
public static class ManagementChallengeProofPayload
{
    private static readonly byte[] DomainSeparator =
        "ApiKit.Management.ChallengeProof"u8.ToArray();

    private const byte FormatVersion = 1;

    /// <summary>
    /// Создаёт канонический набор байт для подписи или проверки proof.
    /// </summary>
    /// <param name="challenge">Проверяемый challenge.</param>
    /// <param name="responder">Identity участника, формирующего ответ.</param>
    /// <param name="credentialId">Публичный идентификатор credential.</param>
    /// <param name="createdAtUtc">Время формирования ответа в UTC.</param>
    /// <returns>Новый массив с каноническим представлением контекста proof.</returns>
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

        var byteCount = Encoding.UTF8.GetByteCount(value);
        WriteInt32(stream, byteCount);

        if (byteCount == 0)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
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
