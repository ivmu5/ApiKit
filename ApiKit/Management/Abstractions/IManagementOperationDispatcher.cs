using System.Security.Claims;
using System.Text.Json;
using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management operation dispatcher.
/// </summary>
public interface IManagementOperationDispatcher
{
    /// <summary>
    /// Executes async.
    /// </summary>
    /// <param name="operationName">The registered operation name.</param>
    /// <param name="request">The operation request.</param>
    /// <param name="principal">The authenticated caller principal.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<ManagementOperationExecutionResult> ExecuteAsync(
        string operationName,
        JsonElement? request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
