namespace ApiKit.Management.Security;

/// <summary>
/// Результат криптографической проверки management peer.
/// </summary>
public sealed record ManagementAuthenticationResult
{
    /// <summary>Показывает, была ли identity peer успешно подтверждена.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>Подтверждённая identity peer при успешной проверке.</summary>
    public ManagementPeerIdentity? Identity { get; init; }

    /// <summary>Идентификатор credential, которым peer подтвердил свою identity.</summary>
    public string? CredentialId { get; init; }

    /// <summary>Стабильный машинно-читаемый код причины отказа.</summary>
    public string? FailureCode { get; init; }

    /// <summary>Диагностическое описание причины отказа. Не должно содержать секретный key material.</summary>
    public string? FailureReason { get; init; }
}
