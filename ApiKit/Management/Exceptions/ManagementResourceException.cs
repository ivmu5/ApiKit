using System.Text.Json;

namespace ApiKit.Management.Exceptions;

/// <summary>
/// Defines an error raised by management resource exception.
/// </summary>
public sealed class ManagementResourceException : Exception
{
    /// <summary>
    /// Initializes a new ManagementResourceException instance.
    /// </summary>
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

    /// <summary>
    /// Gets status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets error code.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Gets payload.
    /// </summary>
    public JsonElement? Payload { get; }
}
