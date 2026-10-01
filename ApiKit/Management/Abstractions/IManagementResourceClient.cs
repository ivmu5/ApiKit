using System.Text.Json;
using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management resource client.
/// </summary>
public interface IManagementResourceClient
{
    /// <summary>
    /// Retrieves a paginated list of management resources.
    /// </summary>
    ValueTask<ManagementResourcePage> ListAsync(
        string instanceId,
        string resourceName,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns async.
    /// </summary>
    ValueTask<JsonElement?> GetAsync(
        string instanceId,
        string resourceName,
        JsonElement key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates async.
    /// </summary>
    ValueTask<JsonElement> CreateAsync(
        string instanceId,
        string resourceName,
        JsonElement model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates async.
    /// </summary>
    ValueTask UpdateAsync(
        string instanceId,
        string resourceName,
        JsonElement key,
        JsonElement model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a JSON Patch to the selected resource.
    /// </summary>
    ValueTask<JsonElement> PatchAsync(
        string instanceId,
        string resourceName,
        JsonElement key,
        JsonElement patchDocument,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes async.
    /// </summary>
    ValueTask DeleteAsync(
        string instanceId,
        string resourceName,
        JsonElement key,
        CancellationToken cancellationToken = default);
}
