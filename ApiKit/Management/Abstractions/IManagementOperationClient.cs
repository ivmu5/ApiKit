using System.Text.Json;
using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management operation client.
/// </summary>
public interface IManagementOperationClient
{
    /// <summary>
    /// Executes async.
    /// </summary>
    ValueTask<ManagementOperationExecutionResult> ExecuteAsync(
        string instanceId,
        string operationName,
        JsonElement? request,
        CancellationToken cancellationToken = default);
}
