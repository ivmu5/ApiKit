using System.IO.Pipes;
using System.Security.Claims;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Windows.Options;
using ApiKit.Management.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Windows.Internal;

internal sealed class WindowsNamedPipeServiceServer(
    IManagementServiceIdentityProvider identityProvider,
    IManagementOperationDispatcher operationDispatcher,
    IManagementResourceDispatcher resourceDispatcher,
    IManagementCredentialProvider credentialProvider,
    IManagementChallengeProvider challengeProvider,
    IManagementPeerVerifier peerVerifier,
    IManagementPeerAuthenticator authenticator,
    IAuthorizationService authorizationService,
    IOptions<WindowsManagementServiceOptions> options,
    ILogger<WindowsNamedPipeServiceServer> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var pipeName = WindowsNamedPipeNames.GetServicePipeName(
            settings.ManagementPipePrefix,
            identityProvider.GetIdentity());

        while (!stoppingToken.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;

            try
            {
                pipe = WindowsNamedPipeSecurityFactory.CreateServer(
                    pipeName,
                    settings.ManagementPipeAccess);

                await pipe.WaitForConnectionAsync(stoppingToken);
                _ = HandleConnectionAsync(pipe, pipeName, settings.MaxMessageBytes, stoppingToken);
                pipe = null;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Ошибка Named Pipe management server {PipeName}.", pipeName);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            finally
            {
                if (pipe is not null)
                {
                    await pipe.DisposeAsync();
                }
            }
        }
    }

    private async Task HandleConnectionAsync(
        NamedPipeServerStream pipe,
        string pipeName,
        int maxMessageBytes,
        CancellationToken stoppingToken)
    {
        await using (pipe)
        {
            try
            {
                var settings = options.Value;
                var serviceIdentity = ManagementPeerIdentity.ForManagedService(
                    identityProvider.GetIdentity());
                var expectedHostIdentity = ManagementPeerIdentity.ForManagementHost(
                    settings.ManagementHostPeerId);
                var transportPeer = WindowsNamedPipeAuthentication.CreatePeerContext(
                    pipe,
                    pipeName,
                    WindowsManagementTransportDefaults.ManagementPurpose);

                // Management Host сначала доказывает identity. До этого сервис не читает
                // delegated claims и не выполняет management-команды.
                var hostAuthentication = await WindowsNamedPipeChallengeResponseProtocol.VerifyIdentityAsync(
                    pipe,
                    serviceIdentity,
                    transportPeer,
                    challengeProvider,
                    peerVerifier,
                    WindowsManagementTransportDefaults.ManagementPurpose,
                    maxMessageBytes,
                    stoppingToken,
                    identity => ManagementPeerIdentitySecurityComparer.Equals(
                        identity,
                        expectedHostIdentity));

                if (!hostAuthentication.Succeeded ||
                    hostAuthentication.Identity is null ||
                    string.IsNullOrWhiteSpace(hostAuthentication.CredentialId))
                {
                    // Security result уже отправлен challenge-response протоколом.
                    return;
                }

                // После проверки Host сервис подтверждает собственную identity этому же Host.
                // Это не позволяет proxy-клиенту получить management request от Host, не доказав
                // соответствие зарегистрированной service identity.
                var serviceAuthentication = await WindowsNamedPipeChallengeResponseProtocol.ProveIdentityAsync(
                    pipe,
                    serviceIdentity,
                    credentialProvider,
                    WindowsManagementTransportDefaults.ManagementPurpose,
                    maxMessageBytes,
                    stoppingToken,
                    expectedChallengeIssuer: expectedHostIdentity);

                if (!serviceAuthentication.Succeeded)
                {
                    return;
                }

                var verifiedPeer = transportPeer with
                {
                    VerifiedIdentity = hostAuthentication.Identity,
                    VerifiedCredentialId = hostAuthentication.CredentialId
                };

                var hostPrincipal = await WindowsNamedPipeAuthentication.AuthenticateAsync(
                    verifiedPeer,
                    authenticator,
                    stoppingToken);

                if (hostPrincipal is null)
                {
                    await WriteFailureAsync(
                        pipe,
                        "unauthorized",
                        "Management host не аутентифицирован.",
                        maxMessageBytes,
                        stoppingToken);
                    return;
                }

                var hostAuthorization = await authorizationService.AuthorizeAsync(
                    hostPrincipal,
                    resource: null,
                    ApiKitManagementAuthorizationDefaults.ManagementHostPolicyName);

                if (!hostAuthorization.Succeeded)
                {
                    await WriteFailureAsync(
                        pipe,
                        "forbidden",
                        "Management peer не является доверенным management host.",
                        maxMessageBytes,
                        stoppingToken);
                    return;
                }

                var request = await WindowsNamedPipeMessageSerializer.ReadAsync<WindowsNamedPipeRequest>(
                    pipe,
                    maxMessageBytes,
                    stoppingToken);

                if (request.ProtocolVersion != WindowsNamedPipeProtocol.Version)
                {
                    await WriteFailureAsync(
                        pipe,
                        "protocol_version",
                        "Версия management transport protocol не поддерживается.",
                        maxMessageBytes,
                        stoppingToken);
                    return;
                }

                var response = request.Type switch
                {
                    WindowsNamedPipeRequestTypes.ExecuteServiceOperation
                        => await HandleOperationAsync(request, hostPrincipal, stoppingToken),
                    WindowsNamedPipeRequestTypes.ExecuteServiceResource
                        => await HandleResourceAsync(request, hostPrincipal, stoppingToken),
                    _ => new WindowsNamedPipeResponse
                    {
                        Succeeded = false,
                        ErrorCode = "unsupported_request",
                        ErrorMessage = "Тип management-запроса не поддерживается."
                    }
                };

                await WindowsNamedPipeMessageSerializer.WriteAsync(
                    pipe,
                    response,
                    maxMessageBytes,
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Ошибка обработки management-запроса через Named Pipe.");
            }
        }
    }

    private async ValueTask<WindowsNamedPipeResponse> HandleOperationAsync(
        WindowsNamedPipeRequest request,
        ClaimsPrincipal hostPrincipal,
        CancellationToken cancellationToken)
    {
        var payload = WindowsNamedPipeMessageSerializer.FromElement<ExecuteServiceOperationRequest>(request.Payload);
        if (payload is null)
        {
            return Failure("invalid_request", "Отсутствуют параметры операции.");
        }

        var delegatedPrincipal = CreateDelegatedPrincipal(payload.Claims) ?? hostPrincipal;
        var result = await operationDispatcher.ExecuteAsync(
            payload.OperationName,
            payload.Request,
            delegatedPrincipal,
            cancellationToken);

        return Success(result);
    }

    private async ValueTask<WindowsNamedPipeResponse> HandleResourceAsync(
        WindowsNamedPipeRequest request,
        ClaimsPrincipal hostPrincipal,
        CancellationToken cancellationToken)
    {
        var payload = WindowsNamedPipeMessageSerializer.FromElement<ExecuteServiceResourceRequest>(request.Payload);
        if (payload is null)
        {
            return Failure("invalid_request", "Отсутствуют параметры CRUD-resource запроса.");
        }

        var delegatedPrincipal = CreateDelegatedPrincipal(payload.Claims) ?? hostPrincipal;
        var result = await resourceDispatcher.ExecuteAsync(
            payload.ResourceName,
            payload.Operation,
            delegatedPrincipal,
            payload.Key,
            payload.Model,
            payload.Page,
            payload.PageSize,
            cancellationToken);

        return Success(result);
    }

    private static ClaimsPrincipal? CreateDelegatedPrincipal(IReadOnlyList<SerializedClaim> claims)
    {
        if (claims.Count == 0)
        {
            return null;
        }

        var identity = new ClaimsIdentity(
            claims.Select(static claim => claim.ToClaim()),
            authenticationType: "ApiKit.Management.Host");

        return new ClaimsPrincipal(identity);
    }

    private static WindowsNamedPipeResponse Success<T>(T payload) => new()
    {
        Succeeded = true,
        Payload = WindowsNamedPipeMessageSerializer.ToElement(payload)
    };

    private static WindowsNamedPipeResponse Failure(string code, string message) => new()
    {
        Succeeded = false,
        ErrorCode = code,
        ErrorMessage = message
    };

    private static ValueTask WriteFailureAsync(
        Stream pipe,
        string code,
        string message,
        int maxMessageBytes,
        CancellationToken cancellationToken) =>
        WindowsNamedPipeMessageSerializer.WriteAsync(
            pipe,
            Failure(code, message),
            maxMessageBytes,
            cancellationToken);
}
