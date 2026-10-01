using System.Text.Json;

namespace ApiKit.Management.Models;

/// <summary>
/// Defines a result of management resource execution result.
/// </summary>
public sealed record ManagementResourceExecutionResult
{
    /// <summary>
    /// Gets or sets whether succeeded is enabled.
    /// </summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// Gets or sets status code.
    /// </summary>
    public required int StatusCode { get; init; }

    /// <summary>
    /// Gets or sets payload.
    /// </summary>
    public JsonElement? Payload { get; init; }

    /// <summary>
    /// Gets or sets error code.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets or sets error message.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
