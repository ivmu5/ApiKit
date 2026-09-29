namespace ApiKit.Management.Models;

/// <summary>
/// Полный снимок management-возможностей одного экземпляра сервиса.
/// </summary>
public sealed record ManagementServiceDescriptor
{
    /// <summary>Идентификация экземпляра сервиса.</summary>
    public required ManagementServiceIdentity Identity { get; init; }

    /// <summary>Адреса ASP.NET Core server, обнаруженные после запуска приложения.</summary>
    public IReadOnlyList<ManagementServiceAddress> Addresses { get; init; } = [];

    /// <summary>Management-транспорты, через которые доступен экземпляр сервиса.</summary>
    public IReadOnlyList<ManagementTransportDescriptor> Transports { get; init; } = [];

    /// <summary>Обнаруженные HTTP endpoints.</summary>
    public IReadOnlyList<ManagementEndpointDescriptor> Endpoints { get; init; } = [];

    /// <summary>Обнаруженные CRUD-ресурсы.</summary>
    public IReadOnlyList<ManagementResourceDescriptor> Resources { get; init; } = [];

    /// <summary>Явно зарегистрированные management-операции.</summary>
    public IReadOnlyList<ManagementOperationDescriptor> Operations { get; init; } = [];

    /// <summary>Имена возможностей, поддерживаемых сервисом.</summary>
    public IReadOnlyList<string> Capabilities { get; init; } = [];

    /// <summary>Дополнительные пользовательские метаданные сервиса.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();
}
