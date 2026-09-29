using System.Text.Json;

namespace ApiKit.Management.Models;

/// <summary>
/// Результат выполнения CRUD-resource запроса внутри management plane.
/// </summary>
/// <remarks>
/// <see cref="StatusCode"/> соответствует HTTP-статусу, который сформировал штатный
/// ASP.NET Core MVC pipeline целевого CRUD action.
/// </remarks>
public sealed record ManagementResourceExecutionResult
{
    /// <summary>Указывает, завершился ли запрос HTTP-статусом 2xx.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>HTTP-статус, сформированный CRUD action.</summary>
    public required int StatusCode { get; init; }

    /// <summary>JSON-тело ответа, если action его сформировал.</summary>
    public JsonElement? Payload { get; init; }

    /// <summary>Машиночитаемый код ошибки management bridge.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Описание ошибки management bridge.</summary>
    public string? ErrorMessage { get; init; }
}
