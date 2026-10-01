using System.Security.Claims;
using System.Text.Json;
using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management resource dispatcher.
/// </summary>
public interface IManagementResourceDispatcher
{
    /// <summary>
    /// Executes async.
    /// </summary>
    /// <param name="resourceName">The management resource name.</param>
    /// <param name="operation">The requested operation.</param>
    /// <param name="principal">The authenticated caller principal.</param>
    /// <param name="key">The resource key.</param>
    /// <param name="model">The input data model.</param>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The maximum number of items per page.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    ValueTask<ManagementResourceExecutionResult> ExecuteAsync(
        string resourceName,
        ManagementResourceOperation operation,
        ClaimsPrincipal principal,
        JsonElement? key = null,
        JsonElement? model = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);
}
