namespace ApiKit.Management.Security;

/// <summary>
/// Содержит доказательство владения credential в ответ на <see cref="ManagementChallenge"/>.
/// </summary>
public sealed record ManagementChallengeResponse
{
    /// <summary>Идентификатор challenge, для которого сформирован ответ.</summary>
    public required string ChallengeId { get; init; }

    /// <summary>Identity участника, сформировавшего proof.</summary>
    public required ManagementPeerIdentity Responder { get; init; }

    /// <summary>
    /// Идентификатор credential/key, использованного для proof. Позволяет поддерживать ротацию без изменения protocol contract.
    /// </summary>
    public required string CredentialId { get; init; }

    /// <summary>
    /// Непрозрачное для transport доказательство владения credential, например цифровая подпись challenge context.
    /// </summary>
    public required byte[] Proof { get; init; }

    /// <summary>Время формирования ответа в UTC.</summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }
}
