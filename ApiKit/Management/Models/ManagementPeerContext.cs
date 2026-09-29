using ApiKit.Management.Security;

namespace ApiKit.Management.Models;

/// <summary>
/// Транспортно-независимые сведения о локальном management-клиенте.
/// </summary>
public sealed record ManagementPeerContext
{
    /// <summary>Имя используемого transport, например <c>named-pipe</c> или <c>unix-socket</c>.</summary>
    public required string Transport { get; init; }

    /// <summary>
    /// Идентичность, подтверждённая операционной системой или transport-уровнем, если она доступна.
    /// </summary>
    public string? OperatingSystemIdentity { get; init; }

    /// <summary>
    /// Identity, подтверждённая management challenge/credential проверкой, если криптографическая
    /// аутентификация уже была выполнена transport-адаптером.
    /// </summary>
    public ManagementPeerIdentity? VerifiedIdentity { get; init; }

    /// <summary>
    /// Идентификатор credential, которым была подтверждена <see cref="VerifiedIdentity"/>.
    /// </summary>
    public string? VerifiedCredentialId { get; init; }

    /// <summary>Дополнительные transport-specific свойства соединения.</summary>
    public IReadOnlyDictionary<string, string> Properties { get; init; }
        = new Dictionary<string, string>();
}
