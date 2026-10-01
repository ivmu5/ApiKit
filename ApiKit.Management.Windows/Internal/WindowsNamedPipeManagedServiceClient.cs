using System.Security.Claims;
using System.Text.Json;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;
using ApiKit.Management.Security;
using ApiKit.Management.Windows.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Windows.Internal;

/// <summary>
/// Forwards authenticated management requests from the host to registered services.
/// </summary>
internal sealed class WindowsNamedPipeManagedServiceClient(
    IManagementServiceCatalog catalog,
    IManagementCredentialProvider credentialProvider,
    IManagementChallengeProvider challengeProvider,
    IManagementPeerVerifier peerVerifier,
    IOptions<WindowsManagementHostOptions> hostOptions,
    ILogger<WindowsNamedPipeManagedServiceClient> logger)
{
    public async ValueTask<ManagementOperationExecutionResult> ExecuteOperationAsync(
        string instanceId,
        string operationName,
        JsonElement? request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await SendToServiceAsync(
                instanceId,
                WindowsNamedPipeRequestTypes.ExecuteServiceOperation,
                new ExecuteServiceOperationRequest(
                    operationName,
                    request,
                    principal.Claims.Select(SerializedClaim.FromClaim).ToArray()),
                cancellationToken);

            if (!response.Succeeded)
            {
                return OperationFailure(
                    response.ErrorCode ?? "transport_error",
                    response.ErrorMessage ?? "Management transport отклонил операцию.");
            }

            return WindowsNamedPipeMessageSerializer.FromElement<ManagementOperationExecutionResult>(response.Payload)
                ?? OperationFailure("invalid_response", "Сервис вернул некорректный результат management-операции.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Не удалось выполнить management-операцию {OperationName} на экземпляре {InstanceId}.",
                operationName,
                instanceId);

            return OperationFailure(
                "transport_error",
                "Не удалось связаться с management endpoint сервиса.");
        }
    }

    public async ValueTask<ManagementResourceExecutionResult> ExecuteResourceAsync(
        string instanceId,
        string resourceName,
        ManagementResourceOperation operation,
        JsonElement? key,
        JsonElement? model,
        int page,
        int pageSize,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await SendToServiceAsync(
                instanceId,
                WindowsNamedPipeRequestTypes.ExecuteServiceResource,
                new ExecuteServiceResourceRequest(
                    resourceName,
                    operation,
                    key,
                    model,
                    page,
                    pageSize,
                    principal.Claims.Select(SerializedClaim.FromClaim).ToArray()),
                cancellationToken);

            if (!response.Succeeded)
            {
                return ResourceFailure(
                    StatusCodes.Status502BadGateway,
                    response.ErrorCode ?? "transport_error",
                    response.ErrorMessage ?? "Management transport отклонил CRUD-resource запрос.");
            }

            return WindowsNamedPipeMessageSerializer.FromElement<ManagementResourceExecutionResult>(response.Payload)
                ?? ResourceFailure(
                    StatusCodes.Status502BadGateway,
                    "invalid_response",
                    "Сервис вернул некорректный результат CRUD-resource запроса.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Не удалось выполнить CRUD-resource операцию {Operation} для {ResourceName} на экземпляре {InstanceId}.",
                operation,
                resourceName,
                instanceId);

            return ResourceFailure(
                StatusCodes.Status502BadGateway,
                "transport_error",
                "Не удалось связаться с management endpoint сервиса.");
        }
    }

    private async ValueTask<WindowsNamedPipeResponse> SendToServiceAsync<TPayload>(
        string instanceId,
        string requestType,
        TPayload payload,
        CancellationToken cancellationToken)
    {
        var service = await catalog.FindAsync(instanceId, cancellationToken);
        if (service is null)
        {
            return Failure("service_not_found", "Экземпляр сервиса не найден в management registry.");
        }

        var transport = service.Descriptor.Transports.FirstOrDefault(static transport =>
            string.Equals(
                transport.Transport,
                WindowsManagementTransportDefaults.TransportName,
                StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                transport.Purpose,
                WindowsManagementTransportDefaults.ManagementPurpose,
                StringComparison.OrdinalIgnoreCase));

        if (transport is null)
        {
            return Failure(
                "transport_not_available",
                "Сервис не опубликовал Named Pipe transport для management-запросов.");
        }

        var settings = hostOptions.Value;
        await using var pipe = await WindowsNamedPipeClientConnection.ConnectAsync(
            transport.Endpoint,
            settings.ServiceConnectTimeout,
            cancellationToken);

        var hostIdentity = ManagementPeerIdentity.ForManagementHost(
            settings.ManagementHostPeerId);
        var expectedServiceIdentity = ManagementPeerIdentity.ForManagedService(
            service.Descriptor.Identity);

        var hostAuthentication = await WindowsNamedPipeChallengeResponseProtocol.ProveIdentityAsync(
            pipe,
            hostIdentity,
            credentialProvider,
            WindowsManagementTransportDefaults.ManagementPurpose,
            settings.MaxMessageBytes,
            cancellationToken,
            expectedChallengeIssuer: expectedServiceIdentity);

        if (!hostAuthentication.Succeeded)
        {
            return Failure(
                hostAuthentication.FailureCode ?? "host_authentication_failed",
                hostAuthentication.FailureReason ?? "Сервис отклонил identity management host.");
        }

        var transportPeer = WindowsNamedPipeAuthentication.CreateClientPeerContext(
            transport.Endpoint,
            WindowsManagementTransportDefaults.ManagementPurpose);

        var serviceAuthentication = await WindowsNamedPipeChallengeResponseProtocol.VerifyIdentityAsync(
            pipe,
            hostIdentity,
            transportPeer,
            challengeProvider,
            peerVerifier,
            WindowsManagementTransportDefaults.ManagementPurpose,
            settings.MaxMessageBytes,
            cancellationToken,
            identity => ManagementPeerIdentitySecurityComparer.Equals(
                identity,
                expectedServiceIdentity));

        if (!serviceAuthentication.Succeeded)
        {
            return Failure(
                serviceAuthentication.FailureCode ?? "service_authentication_failed",
                serviceAuthentication.FailureReason ?? "Сервис не подтвердил ожидаемую identity.");
        }

        await WindowsNamedPipeMessageSerializer.WriteAsync(
            pipe,
            new WindowsNamedPipeRequest
            {
                Type = requestType,
                Payload = WindowsNamedPipeMessageSerializer.ToElement(payload)
            },
            settings.MaxMessageBytes,
            cancellationToken);

        var response = await WindowsNamedPipeMessageSerializer.ReadAsync<WindowsNamedPipeResponse>(
            pipe,
            settings.MaxMessageBytes,
            cancellationToken);

        if (response.ProtocolVersion != WindowsNamedPipeProtocol.Version)
        {
            return Failure(
                "protocol_version",
                "Сервис использует несовместимую версию transport protocol.");
        }

        return response;
    }

    private static WindowsNamedPipeResponse Failure(string code, string message) => new()
    {
        Succeeded = false,
        ErrorCode = code,
        ErrorMessage = message
    };

    private static ManagementOperationExecutionResult OperationFailure(string code, string message) => new()
    {
        Succeeded = false,
        ErrorCode = code,
        ErrorMessage = message
    };

    private static ManagementResourceExecutionResult ResourceFailure(
        int statusCode,
        string code,
        string message) => new()
        {
            Succeeded = false,
            StatusCode = statusCode,
            ErrorCode = code,
            ErrorMessage = message
        };
}
