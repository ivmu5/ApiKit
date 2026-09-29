namespace ApiKit.Management.Models;

/// <summary>
/// Описывает management-транспорт, через который можно обратиться к экземпляру сервиса.
/// </summary>
/// <param name="Transport">Стабильное имя транспорта, например <c>named-pipe</c>.</param>
/// <param name="Endpoint">Transport-specific адрес, например имя Named Pipe.</param>
public sealed record ManagementTransportDescriptor(
    string Transport,
    string Endpoint)
{
    /// <summary>Назначение endpoint, например <c>operations</c>.</summary>
    public string? Purpose { get; init; }

    /// <summary>Дополнительная transport-specific metadata.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();
}
