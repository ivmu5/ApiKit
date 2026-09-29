using System.Text.Json;

namespace ApiKit.Management.Exceptions;

/// <summary>
/// Ошибка выполнения CRUD-resource операции через management plane.
/// </summary>
public sealed class ManagementResourceException : Exception
{
    /// <summary>Создаёт исключение из ответа управляемого сервиса.</summary>
    public ManagementResourceException(
        int statusCode,
        string? errorCode,
        string? message,
        JsonElement? payload = null)
        : base(message ?? $"Management resource request завершился со статусом {statusCode}.")
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Payload = payload;
    }

    /// <summary>HTTP-статус штатного CRUD pipeline.</summary>
    public int StatusCode { get; }

    /// <summary>Машиночитаемый код transport/bridge ошибки.</summary>
    public string? ErrorCode { get; }

    /// <summary>JSON-тело ошибки, включая ProblemDetails, если оно было сформировано API.</summary>
    public JsonElement? Payload { get; }
}
