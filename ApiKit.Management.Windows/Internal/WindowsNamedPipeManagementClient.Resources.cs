using System.Text.Json;
using ApiKit.Management.Exceptions;
using ApiKit.Management.Models;
using Microsoft.AspNetCore.Http;

namespace ApiKit.Management.Windows.Internal;

internal sealed partial class WindowsNamedPipeManagementClient
{
    public async ValueTask<ManagementResourcePage> ListAsync(
        string instanceId,
        string resourceName,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        ValidateResourceArguments(instanceId, resourceName);

        var result = await ExecuteResourceAsync(
            instanceId,
            resourceName,
            ManagementResourceOperation.List,
            key: null,
            model: null,
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        EnsureResourceSuccess(result);

        return WindowsNamedPipeMessageSerializer.FromElement<ManagementResourcePage>(result.Payload)
            ?? throw new ManagementResourceException(
                result.StatusCode,
                "invalid_response",
                "Сервис вернул пустую или некорректную страницу CRUD-ресурса.");
    }

    public async ValueTask<JsonElement?> GetAsync(
        string instanceId,
        string resourceName,
        JsonElement key,
        CancellationToken cancellationToken = default)
    {
        ValidateResourceArguments(instanceId, resourceName);

        var result = await ExecuteResourceAsync(
            instanceId,
            resourceName,
            ManagementResourceOperation.Get,
            key: key,
            model: null,
            page: 1,
            pageSize: 1,
            cancellationToken: cancellationToken);

        if (result.StatusCode == StatusCodes.Status404NotFound)
        {
            return null;
        }

        EnsureResourceSuccess(result);
        return result.Payload;
    }

    public async ValueTask<JsonElement> CreateAsync(
        string instanceId,
        string resourceName,
        JsonElement model,
        CancellationToken cancellationToken = default)
    {
        ValidateResourceArguments(instanceId, resourceName);

        var result = await ExecuteResourceAsync(
            instanceId,
            resourceName,
            ManagementResourceOperation.Create,
            key: null,
            model: model,
            page: 1,
            pageSize: 1,
            cancellationToken: cancellationToken);

        EnsureResourceSuccess(result);
        return RequirePayload(result);
    }

    public async ValueTask UpdateAsync(
        string instanceId,
        string resourceName,
        JsonElement key,
        JsonElement model,
        CancellationToken cancellationToken = default)
    {
        ValidateResourceArguments(instanceId, resourceName);

        var result = await ExecuteResourceAsync(
            instanceId,
            resourceName,
            ManagementResourceOperation.Update,
            key: key,
            model: model,
            page: 1,
            pageSize: 1,
            cancellationToken: cancellationToken);

        EnsureResourceSuccess(result);
    }

    public async ValueTask<JsonElement> PatchAsync(
        string instanceId,
        string resourceName,
        JsonElement key,
        JsonElement patchDocument,
        CancellationToken cancellationToken = default)
    {
        ValidateResourceArguments(instanceId, resourceName);

        var result = await ExecuteResourceAsync(
            instanceId,
            resourceName,
            ManagementResourceOperation.Patch,
            key: key,
            model: patchDocument,
            page: 1,
            pageSize: 1,
            cancellationToken: cancellationToken);

        EnsureResourceSuccess(result);
        return RequirePayload(result);
    }

    public async ValueTask DeleteAsync(
        string instanceId,
        string resourceName,
        JsonElement key,
        CancellationToken cancellationToken = default)
    {
        ValidateResourceArguments(instanceId, resourceName);

        var result = await ExecuteResourceAsync(
            instanceId,
            resourceName,
            ManagementResourceOperation.Delete,
            key: key,
            model: null,
            page: 1,
            pageSize: 1,
            cancellationToken: cancellationToken);

        EnsureResourceSuccess(result);
    }

    private async ValueTask<ManagementResourceExecutionResult> ExecuteResourceAsync(
        string instanceId,
        string resourceName,
        ManagementResourceOperation operation,
        JsonElement? key,
        JsonElement? model,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var response = await SendAsync(
            WindowsNamedPipeRequestTypes.ExecuteResource,
            new ExecuteResourceRequest(
                instanceId,
                resourceName,
                operation,
                key,
                model,
                page,
                pageSize),
            cancellationToken);

        return ReadPayload<ManagementResourceExecutionResult>(response)
            ?? new ManagementResourceExecutionResult
            {
                Succeeded = false,
                StatusCode = StatusCodes.Status502BadGateway,
                ErrorCode = "invalid_response",
                ErrorMessage = "Management host вернул пустой результат CRUD-resource операции."
            };
    }

    private static void ValidateResourceArguments(string instanceId, string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
    }

    private static void EnsureResourceSuccess(ManagementResourceExecutionResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new ManagementResourceException(
            result.StatusCode,
            result.ErrorCode,
            result.ErrorMessage,
            result.Payload);
    }

    private static JsonElement RequirePayload(ManagementResourceExecutionResult result) =>
        result.Payload ?? throw new ManagementResourceException(
            result.StatusCode,
            "empty_response",
            "CRUD-resource операция успешно завершилась, но сервис не вернул JSON-модель.");
}
