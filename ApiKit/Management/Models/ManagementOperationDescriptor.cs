namespace ApiKit.Management.Models;

/// <summary>
/// Описывает произвольную management-операцию сервиса.
/// </summary>
public sealed record ManagementOperationDescriptor
{
    /// <summary>Стабильное техническое имя операции.</summary>
    public required string Name { get; init; }

    /// <summary>Отображаемое имя операции.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Описание назначения операции.</summary>
    public string? Description { get; init; }

    /// <summary>Группа операции.</summary>
    public string? GroupName { get; init; }

    /// <summary>Имя типа входной модели.</summary>
    public required string RequestTypeName { get; init; }

    /// <summary>Имя типа результата.</summary>
    public required string ResultTypeName { get; init; }

    /// <summary>ASP.NET Core authorization policy, необходимая для выполнения операции.</summary>
    public required string AuthorizationPolicy { get; init; }
}
