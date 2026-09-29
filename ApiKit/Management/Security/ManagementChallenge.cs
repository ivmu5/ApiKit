namespace ApiKit.Management.Security;

/// <summary>
/// Представляет одноразовый challenge для подтверждения владения management credential.
/// </summary>
/// <remarks>
/// При формировании криптографического proof необходимо связывать с подписью весь контекст challenge,
/// а не только <see cref="Nonce"/>. Это предотвращает перенос корректного proof между разными transport,
/// назначениями протокола или участниками management plane.
/// </remarks>
public sealed record ManagementChallenge
{
    /// <summary>Уникальный идентификатор challenge.</summary>
    public required string ChallengeId { get; init; }

    /// <summary>Участник, выпустивший challenge.</summary>
    public required ManagementPeerIdentity Issuer { get; init; }

    /// <summary>Участник, который должен подтвердить владение credential.</summary>
    public required ManagementPeerIdentity Subject { get; init; }

    /// <summary>Криптографически случайное одноразовое значение.</summary>
    public required byte[] Nonce { get; init; }

    /// <summary>Transport, в рамках которого выпущен challenge.</summary>
    public required string Transport { get; init; }

    /// <summary>Назначение соединения, например registration, administration или management.</summary>
    public required string Purpose { get; init; }

    /// <summary>Версия management transport protocol, для которой выпущен challenge.</summary>
    public required int ProtocolVersion { get; init; }

    /// <summary>Время выпуска challenge в UTC.</summary>
    public required DateTimeOffset IssuedAtUtc { get; init; }

    /// <summary>Момент, после которого challenge не должен приниматься.</summary>
    public required DateTimeOffset ExpiresAtUtc { get; init; }
}
