using System.Text.Json;

namespace ApiKit.Management.Models;

/// <summary>
/// Defines a result of management operation execution result.
/// </summary>
public sealed record ManagementOperationExecutionResult
{
    /// <summary>
    /// Gets or sets whether succeeded is enabled.
    /// </summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// Gets or sets value.
    /// </summary>
    public JsonElement? Value { get; init; }

    /// <summary>
    /// Gets or sets error code.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets or sets error message.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
