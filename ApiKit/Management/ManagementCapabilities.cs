namespace ApiKit.Management;

/// <summary>
/// Стандартные имена возможностей management plane.
/// </summary>
public static class ManagementCapabilities
{
    /// <summary>Сервис публикует адреса ASP.NET Core server.</summary>
    public const string Addresses = "addresses";

    /// <summary>Сервис публикует metadata HTTP endpoints.</summary>
    public const string Endpoints = "endpoints";

    /// <summary>Сервис публикует CRUD-ресурсы.</summary>
    public const string Resources = "resources";

    /// <summary>Сервис предоставляет произвольные management-операции.</summary>
    public const string Operations = "operations";

    /// <summary>Сервис предоставляет health metadata или проверки.</summary>
    public const string Health = "health";

    /// <summary>Сервис предоставляет управляемую конфигурацию.</summary>
    public const string Configuration = "configuration";

    /// <summary>Сервис предоставляет diagnostics metadata.</summary>
    public const string Diagnostics = "diagnostics";

    /// <summary>Сервис предоставляет журналы через management plane.</summary>
    public const string Logs = "logs";

    /// <summary>Сервис предоставляет метрики через management plane.</summary>
    public const string Metrics = "metrics";

    /// <summary>Management host умеет управлять жизненным циклом сервисов.</summary>
    public const string Lifecycle = "lifecycle";

    /// <summary>Management host умеет устанавливать и удалять сервисы.</summary>
    public const string Installation = "installation";
}
