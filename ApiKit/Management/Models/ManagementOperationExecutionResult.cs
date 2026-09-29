using System.Text.Json;

namespace ApiKit.Management.Models;

/// <summary>
/// Результат выполнения management-операции через универсальный dispatcher.
/// </summary>
public sealed record ManagementOperationExecutionResult
{
    /// <summary>Признак успешного выполнения.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>Сериализованный результат успешной операции.</summary>
    public JsonElement? Value { get; init; }

    /// <summary>Машиночитаемый код ошибки.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Безопасное для передачи клиенту описание ошибки.</summary>
    public string? ErrorMessage { get; init; }
}
