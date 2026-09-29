namespace ApiKit.Management.Models;

/// <summary>
/// Описывает CRUD-ресурс, доступный в сервисе.
/// </summary>
public sealed record ManagementResourceDescriptor
{
    /// <summary>Стабильное техническое имя ресурса.</summary>
    public required string Name { get; init; }

    /// <summary>Отображаемое имя ресурса.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Группа ресурса.</summary>
    public string? GroupName { get; init; }

    /// <summary>Имя контроллера, обслуживающего ресурс.</summary>
    public required string ControllerName { get; init; }

    /// <summary>Полное имя EF entity.</summary>
    public required string EntityTypeName { get; init; }

    /// <summary>Полное имя типа первичного ключа.</summary>
    public required string KeyTypeName { get; init; }

    /// <summary>
    /// JSON-имя свойства read-модели, содержащего ключ ресурса, если оно известно.
    /// </summary>
    public string? KeyJsonName { get; init; }

    /// <summary>Описание read-модели ресурса.</summary>
    public required ManagementModelDescriptor ReadModel { get; init; }

    /// <summary>Описание модели создания ресурса.</summary>
    public required ManagementModelDescriptor CreateModel { get; init; }

    /// <summary>Описание модели обновления ресурса.</summary>
    public required ManagementModelDescriptor UpdateModel { get; init; }

    /// <summary>Доступные стандартные операции.</summary>
    public required IReadOnlyList<ManagementResourceOperation> Operations { get; init; }

    /// <summary>
    /// Итоговые permission-имена для операций, если permission-based authorization подключена в сервисе.
    /// </summary>
    public IReadOnlyDictionary<ManagementResourceOperation, string> Permissions { get; init; } =
        new Dictionary<ManagementResourceOperation, string>();
}
