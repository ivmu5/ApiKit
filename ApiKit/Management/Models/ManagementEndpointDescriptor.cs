namespace ApiKit.Management.Models;

/// <summary>
/// Описывает HTTP endpoint, обнаруженный средствами ASP.NET Core.
/// </summary>
public sealed record ManagementEndpointDescriptor
{
    /// <summary>Уникальное или отображаемое имя endpoint.</summary>
    public required string Name { get; init; }

    /// <summary>Шаблон маршрута endpoint.</summary>
    public required string Route { get; init; }

    /// <summary>Поддерживаемые HTTP-методы.</summary>
    public required IReadOnlyList<string> HttpMethods { get; init; }

    /// <summary>Группа endpoint, если она определена стандартными ASP.NET metadata.</summary>
    public string? GroupName { get; init; }

    /// <summary>Имя MVC-контроллера, если endpoint относится к controller action.</summary>
    public string? ControllerName { get; init; }

    /// <summary>Имя MVC action, если endpoint относится к controller action.</summary>
    public string? ActionName { get; init; }

    /// <summary>Признак наличия <c>AllowAnonymous</c>.</summary>
    public bool AllowsAnonymous { get; init; }

    /// <summary>Явно заданные authorization policies.</summary>
    public IReadOnlyList<string> AuthorizationPolicies { get; init; } = [];

    /// <summary>Явно заданные role-based ограничения.</summary>
    public IReadOnlyList<string> AuthorizationRoles { get; init; } = [];

    /// <summary>Явно заданные authentication schemes.</summary>
    public IReadOnlyList<string> AuthenticationSchemes { get; init; } = [];
}
