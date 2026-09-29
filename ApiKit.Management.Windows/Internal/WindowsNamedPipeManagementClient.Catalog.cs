using System.Text.Json;
using ApiKit.Management.Models;

namespace ApiKit.Management.Windows.Internal;

internal sealed partial class WindowsNamedPipeManagementClient
{
    public async ValueTask<IReadOnlyList<RegisteredManagementService>> GetServicesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(
            WindowsNamedPipeRequestTypes.GetServices,
            cancellationToken);

        return ReadPayload<RegisteredManagementService[]>(response) ?? [];
    }

    public async ValueTask<IReadOnlyList<RegisteredManagementService>> GetServicesAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var response = await SendAsync(
            WindowsNamedPipeRequestTypes.GetServicesByName,
            new ServiceNameRequest(serviceName),
            cancellationToken);

        return ReadPayload<RegisteredManagementService[]>(response) ?? [];
    }

    public async ValueTask<RegisteredManagementService?> FindAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        var response = await SendAsync(
            WindowsNamedPipeRequestTypes.FindService,
            new InstanceRequest(instanceId),
            cancellationToken);

        return ReadPayload<RegisteredManagementService>(response);
    }

    public async ValueTask<ManagementOperationExecutionResult> ExecuteAsync(
        string instanceId,
        string operationName,
        JsonElement? request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        var response = await SendAsync(
            WindowsNamedPipeRequestTypes.ExecuteOperation,
            new ExecuteOperationRequest(instanceId, operationName, request),
            cancellationToken);

        return ReadPayload<ManagementOperationExecutionResult>(response)
            ?? new ManagementOperationExecutionResult
            {
                Succeeded = false,
                ErrorCode = "invalid_response",
                ErrorMessage = "Management host вернул пустой результат операции."
            };
    }
}
