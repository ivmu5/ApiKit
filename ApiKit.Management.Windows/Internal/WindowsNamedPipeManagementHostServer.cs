using System.IO.Pipes;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;
using ApiKit.Management.Security;
using ApiKit.Management.Windows.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Windows.Internal;

internal sealed class WindowsNamedPipeManagementHostServer(
    IManagementServiceRegistry registry,
    IManagementCredentialProvider credentialProvider,
    IManagementPeerAuthenticator authenticator,
    IManagementChallengeProvider challengeProvider,
    IManagementPeerVerifier peerVerifier,
    IAuthorizationService authorizationService,
    WindowsNamedPipeManagedServiceClient serviceClient,
    IManagedServiceLifecycleController lifecycleController,
    IManagedServiceInstaller serviceInstaller,
    WindowsManagedServiceIsolationRegistry isolationRegistry,
    IOptions<WindowsManagementHostOptions> options,
    ILogger<WindowsNamedPipeManagementHostServer> logger)
    : BackgroundService
{
    private readonly SemaphoreSlim _registrationConnectionSlots = new(64, 64);
    private readonly SemaphoreSlim _administrationConnectionSlots = new(64, 64);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        return Task.WhenAll(
            RunServerLoopAsync(
                settings.RegistrationPipeName,
                settings.RegistrationPipeAccess,
                isAdministrationPipe: false,
                stoppingToken),
            RunServerLoopAsync(
                settings.AdministrationPipeName,
                settings.AdministrationPipeAccess,
                isAdministrationPipe: true,
                stoppingToken));
    }

    private async Task RunServerLoopAsync(
        string pipeName,
        WindowsNamedPipeAccessOptions access,
        bool isAdministrationPipe,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;

            try
            {
                var isolationVersion = isolationRegistry.Version;
                IReadOnlyCollection<string> additionalSids =
                    !isAdministrationPipe && options.Value.UseProvisionedServiceSidAcl
                        ? isolationRegistry.GetProvisionedServiceSids()
                        : Array.Empty<string>();

                pipe = WindowsNamedPipeSecurityFactory.CreateServer(
                    pipeName,
                    access,
                    additionalSids);

                if (!isAdministrationPipe && options.Value.UseProvisionedServiceSidAcl)
                {
                    using var waitConnectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    using var aclChangeCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    var waitForConnection = pipe.WaitForConnectionAsync(waitConnectionCancellation.Token);
                    var waitForAclChange = isolationRegistry.WaitForChangeAsync(
                        isolationVersion,
                        aclChangeCancellation.Token);

                    var completed = await Task.WhenAny(waitForConnection, waitForAclChange);
                    if (completed == waitForAclChange)
                    {
                        waitConnectionCancellation.Cancel();

                        try
                        {
                            await waitForConnection;
                        }
                        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                        {
                        }

                        continue;
                    }

                    await waitForConnection;
                    aclChangeCancellation.Cancel();
                }
                else
                {
                    await pipe.WaitForConnectionAsync(stoppingToken);
                }

                var slots = isAdministrationPipe
                    ? _administrationConnectionSlots
                    : _registrationConnectionSlots;
                if (!slots.Wait(0))
                {
                    // Refuse excess connections instead of allocating unlimited
                    // tasks, authentication challenges, and pipe handles.
                    logger.LogWarning("Named Pipe {PipeName} connection limit reached.", pipeName);
                    continue;
                }

                _ = HandleLimitedConnectionAsync(pipe, pipeName, isAdministrationPipe, slots, stoppingToken);
                pipe = null;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Ошибка management host Named Pipe {PipeName}.", pipeName);
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

    private async Task HandleLimitedConnectionAsync(
        NamedPipeServerStream pipe,
        string pipeName,
        bool isAdministrationPipe,
        SemaphoreSlim slots,
        CancellationToken stoppingToken)
    {
        try
        {
            await HandleConnectionAsync(pipe, pipeName, isAdministrationPipe, stoppingToken);
        }
        finally
        {
            slots.Release();
        }
    }

    private async Task HandleConnectionAsync(
        NamedPipeServerStream pipe,
        string pipeName,
        bool isAdministrationPipe,
        CancellationToken stoppingToken)
    {
        await using (pipe)
        {
            try
            {
                if (isAdministrationPipe)
                {
                    await HandleAdministrationConnectionAsync(
                        pipe,
                        pipeName,
                        stoppingToken);
                }
                else
                {
                    await HandleRegistrationConnectionAsync(
                        pipe,
                        pipeName,
                        stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Ошибка обработки Named Pipe management host запроса.");
            }
        }
    }

    private async Task HandleRegistrationConnectionAsync(
        NamedPipeServerStream pipe,
        string pipeName,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var transportPeer = WindowsNamedPipeAuthentication.CreatePeerContext(
            pipe,
            pipeName,
            WindowsManagementTransportDefaults.RegistrationPurpose);

        var hostIdentity = ManagementPeerIdentity.ForManagementHost(
            settings.ManagementHostPeerId);

        var hostAuthentication = await WindowsNamedPipeChallengeResponseProtocol.ProveIdentityAsync(
            pipe,
            hostIdentity,
            credentialProvider,
            WindowsManagementTransportDefaults.RegistrationPurpose,
            settings.MaxMessageBytes,
            cancellationToken,
            challengeIssuerValidator: IsValidManagedServiceIdentity);

        if (!hostAuthentication.Succeeded)
        {
            return;
        }

        var authentication = await WindowsNamedPipeChallengeResponseProtocol.VerifyIdentityAsync(
            pipe,
            hostIdentity,
            transportPeer,
            challengeProvider,
            peerVerifier,
            WindowsManagementTransportDefaults.RegistrationPurpose,
            settings.MaxMessageBytes,
            cancellationToken,
            IsValidManagedServiceIdentity);

        if (!authentication.Succeeded ||
            authentication.Identity is null ||
            string.IsNullOrWhiteSpace(authentication.CredentialId))
        {
            return;
        }

        var verifiedPeer = transportPeer with
        {
            VerifiedIdentity = authentication.Identity,
            VerifiedCredentialId = authentication.CredentialId
        };

        var principal = await WindowsNamedPipeAuthentication.AuthenticateAsync(
            verifiedPeer,
            authenticator,
            cancellationToken);

        if (principal is null)
        {
            await WriteResponseAsync(
                pipe,
                Failure("unauthorized", "Management service peer не аутентифицирован."),
                settings.MaxMessageBytes,
                cancellationToken);
            return;
        }

        var authorization = await authorizationService.AuthorizeAsync(
            principal,
            resource: null,
            ApiKitManagementAuthorizationDefaults.ServiceRegistrationPolicyName);

        if (!authorization.Succeeded)
        {
            await WriteResponseAsync(
                pipe,
                Failure("forbidden", "Management peer не имеет права регистрации сервисов."),
                settings.MaxMessageBytes,
                cancellationToken);
            return;
        }

        var request = await ReadRequestAsync(
            pipe,
            settings.MaxMessageBytes,
            cancellationToken);

        if (request is null)
        {
            return;
        }

        var response = await HandleRegistrationRequestAsync(
            request,
            authentication.Identity,
            cancellationToken);

        await WriteResponseAsync(
            pipe,
            response,
            settings.MaxMessageBytes,
            cancellationToken);
    }

    private async Task HandleAdministrationConnectionAsync(
        NamedPipeServerStream pipe,
        string pipeName,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var transportPeer = WindowsNamedPipeAuthentication.CreatePeerContext(
            pipe,
            pipeName,
            WindowsManagementTransportDefaults.AdministrationPurpose);

        var hostIdentity = ManagementPeerIdentity.ForManagementHost(
            settings.ManagementHostPeerId);

        var hostAuthentication = await WindowsNamedPipeChallengeResponseProtocol.ProveIdentityAsync(
            pipe,
            hostIdentity,
            credentialProvider,
            WindowsManagementTransportDefaults.AdministrationPurpose,
            settings.MaxMessageBytes,
            cancellationToken,
            challengeIssuerValidator: IsValidAdminClientIdentity);

        if (!hostAuthentication.Succeeded)
        {
            return;
        }

        var authentication = await WindowsNamedPipeChallengeResponseProtocol.VerifyIdentityAsync(
            pipe,
            hostIdentity,
            transportPeer,
            challengeProvider,
            peerVerifier,
            WindowsManagementTransportDefaults.AdministrationPurpose,
            settings.MaxMessageBytes,
            cancellationToken,
            IsValidAdminClientIdentity);

        if (!authentication.Succeeded ||
            authentication.Identity is null ||
            string.IsNullOrWhiteSpace(authentication.CredentialId))
        {
            return;
        }

        var verifiedPeer = transportPeer with
        {
            VerifiedIdentity = authentication.Identity,
            VerifiedCredentialId = authentication.CredentialId
        };

        var principal = await WindowsNamedPipeAuthentication.AuthenticateAsync(
            verifiedPeer,
            authenticator,
            cancellationToken);

        if (principal is null)
        {
            await WriteResponseAsync(
                pipe,
                Failure("unauthorized", "Административный management peer не аутентифицирован."),
                settings.MaxMessageBytes,
                cancellationToken);
            return;
        }

        var authorization = await authorizationService.AuthorizeAsync(
            principal,
            resource: null,
            ApiKitManagementAuthorizationDefaults.PolicyName);

        if (!authorization.Succeeded)
        {
            await WriteResponseAsync(
                pipe,
                Failure("forbidden", "Management peer не имеет административных прав."),
                settings.MaxMessageBytes,
                cancellationToken);
            return;
        }

        var request = await ReadRequestAsync(
            pipe,
            settings.MaxMessageBytes,
            cancellationToken);

        if (request is null)
        {
            return;
        }

        var response = await HandleAdministrationRequestAsync(
            request,
            principal,
            cancellationToken);

        await WriteResponseAsync(
            pipe,
            response,
            settings.MaxMessageBytes,
            cancellationToken);
    }

    private async ValueTask<WindowsNamedPipeRequest?> ReadRequestAsync(
        Stream pipe,
        int maxMessageBytes,
        CancellationToken cancellationToken)
    {
        using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        requestTimeout.CancelAfter(TimeSpan.FromSeconds(30));
        var request = await WindowsNamedPipeMessageSerializer.ReadAsync<WindowsNamedPipeRequest>(
            pipe,
            maxMessageBytes,
            requestTimeout.Token);

        if (request.ProtocolVersion == WindowsNamedPipeProtocol.Version)
        {
            return request;
        }

        await WriteResponseAsync(
            pipe,
            Failure("protocol_version", "Версия management transport protocol не поддерживается."),
            maxMessageBytes,
            cancellationToken);

        return null;
    }

    private async ValueTask<WindowsNamedPipeResponse> HandleRegistrationRequestAsync(
        WindowsNamedPipeRequest request,
        ManagementPeerIdentity verifiedIdentity,
        CancellationToken cancellationToken)
    {
        switch (request.Type)
        {
            case WindowsNamedPipeRequestTypes.Register:
            {
                var registration = WindowsNamedPipeMessageSerializer.FromElement<ManagementServiceRegistration>(request.Payload);
                if (registration is null)
                {
                    return Failure("invalid_request", "Отсутствует регистрация сервиса.");
                }

                if (!MatchesVerifiedServiceIdentity(
                        registration.Descriptor.Identity,
                        verifiedIdentity))
                {
                    return Failure(
                        "identity_mismatch",
                        "Регистрация сервиса не соответствует криптографически подтверждённой identity.");
                }

                await registry.UpsertAsync(registration, cancellationToken);
                return Success();
            }

            case WindowsNamedPipeRequestTypes.Withdraw:
            {
                var payload = WindowsNamedPipeMessageSerializer.FromElement<WithdrawRegistrationRequest>(request.Payload);
                if (payload is null)
                {
                    return Failure("invalid_request", "Отсутствует instance id сервиса.");
                }

                if (!StringComparer.Ordinal.Equals(
                        payload.InstanceId,
                        verifiedIdentity.InstanceId))
                {
                    return Failure(
                        "identity_mismatch",
                        "Сервис может удалить из registry только собственный подтверждённый instance.");
                }

                await registry.RemoveAsync(payload.InstanceId, cancellationToken);
                return Success();
            }

            default:
                return Failure("unsupported_request", "Registration pipe поддерживает только регистрацию и withdraw.");
        }
    }

    private static bool IsValidManagedServiceIdentity(ManagementPeerIdentity identity)
    {
        return identity.Kind == ManagementPeerKind.ManagedService &&
               !string.IsNullOrWhiteSpace(identity.PeerId) &&
               !string.IsNullOrWhiteSpace(identity.InstanceId);
    }

    private static bool IsValidAdminClientIdentity(ManagementPeerIdentity identity)
    {
        return identity.Kind == ManagementPeerKind.AdminClient &&
               !string.IsNullOrWhiteSpace(identity.PeerId) &&
               string.IsNullOrWhiteSpace(identity.InstanceId);
    }

    private static bool MatchesVerifiedServiceIdentity(
        ManagementServiceIdentity serviceIdentity,
        ManagementPeerIdentity verifiedIdentity)
    {
        return StringComparer.Ordinal.Equals(
                   serviceIdentity.ServiceName,
                   verifiedIdentity.PeerId) &&
               StringComparer.Ordinal.Equals(
                   serviceIdentity.InstanceId,
                   verifiedIdentity.InstanceId);
    }

    private static ValueTask WriteResponseAsync(
        Stream pipe,
        WindowsNamedPipeResponse response,
        int maxMessageBytes,
        CancellationToken cancellationToken)
    {
        return WindowsNamedPipeMessageSerializer.WriteAsync(
            pipe,
            response,
            maxMessageBytes,
            cancellationToken);
    }

    private async ValueTask<WindowsNamedPipeResponse> HandleAdministrationRequestAsync(
        WindowsNamedPipeRequest request,
        System.Security.Claims.ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        switch (request.Type)
        {
            case WindowsNamedPipeRequestTypes.GetServices:
                return Success(await registry.GetServicesAsync(cancellationToken));

            case WindowsNamedPipeRequestTypes.GetServicesByName:
            {
                var payload = WindowsNamedPipeMessageSerializer.FromElement<ServiceNameRequest>(request.Payload);
                return payload is null
                    ? Failure("invalid_request", "Отсутствует имя сервиса.")
                    : Success(await registry.GetServicesAsync(payload.ServiceName, cancellationToken));
            }

            case WindowsNamedPipeRequestTypes.FindService:
            {
                var payload = WindowsNamedPipeMessageSerializer.FromElement<InstanceRequest>(request.Payload);
                return payload is null
                    ? Failure("invalid_request", "Отсутствует instance id сервиса.")
                    : Success(await registry.FindAsync(payload.InstanceId, cancellationToken));
            }

            case WindowsNamedPipeRequestTypes.ExecuteOperation:
            {
                var payload = WindowsNamedPipeMessageSerializer.FromElement<ExecuteOperationRequest>(request.Payload);
                if (payload is null)
                {
                    return Failure("invalid_request", "Отсутствуют параметры management-операции.");
                }

                var result = await serviceClient.ExecuteOperationAsync(
                    payload.InstanceId,
                    payload.OperationName,
                    payload.Request,
                    principal,
                    cancellationToken);

                return Success(result);
            }

            case WindowsNamedPipeRequestTypes.ExecuteResource:
            {
                var payload = WindowsNamedPipeMessageSerializer.FromElement<ExecuteResourceRequest>(request.Payload);
                if (payload is null)
                {
                    return Failure("invalid_request", "Отсутствуют параметры CRUD-resource запроса.");
                }

                var result = await serviceClient.ExecuteResourceAsync(
                    payload.InstanceId,
                    payload.ResourceName,
                    payload.Operation,
                    payload.Key,
                    payload.Model,
                    payload.Page,
                    payload.PageSize,
                    principal,
                    cancellationToken);

                return Success(result);
            }

            case WindowsNamedPipeRequestTypes.GetServiceState:
            {
                var payload = WindowsNamedPipeMessageSerializer.FromElement<ManagedServiceNameRequest>(request.Payload);
                return payload is null
                    ? Failure("invalid_request", "Отсутствует имя Windows Service.")
                    : Success(await lifecycleController.GetStateAsync(payload.ServiceName, cancellationToken));
            }

            case WindowsNamedPipeRequestTypes.StartService:
            case WindowsNamedPipeRequestTypes.StopService:
            case WindowsNamedPipeRequestTypes.RestartService:
            {
                if (!await HasPolicyAsync(principal, ApiKitManagementAuthorizationDefaults.LifecyclePolicyName))
                {
                    return Failure("forbidden", "Management peer не имеет прав управления жизненным циклом сервисов.");
                }

                var payload = WindowsNamedPipeMessageSerializer.FromElement<ManagedServiceNameRequest>(request.Payload);
                if (payload is null)
                {
                    return Failure("invalid_request", "Отсутствует имя Windows Service.");
                }

                if (request.Type == WindowsNamedPipeRequestTypes.StartService)
                {
                    await lifecycleController.StartAsync(payload.ServiceName, cancellationToken);
                }
                else if (request.Type == WindowsNamedPipeRequestTypes.StopService)
                {
                    await lifecycleController.StopAsync(payload.ServiceName, cancellationToken);
                }
                else
                {
                    await lifecycleController.RestartAsync(payload.ServiceName, cancellationToken);
                }

                return Success();
            }

            case WindowsNamedPipeRequestTypes.InstallService:
            case WindowsNamedPipeRequestTypes.UpdateService:
            {
                if (!await HasPolicyAsync(principal, ApiKitManagementAuthorizationDefaults.PackageManagementPolicyName))
                {
                    return Failure("forbidden", "Management peer не имеет прав установки или обновления сервисов.");
                }

                var payload = WindowsNamedPipeMessageSerializer.FromElement<ManagedServicePackageRequest>(request.Payload);
                if (payload is null)
                {
                    return Failure("invalid_request", "Отсутствуют параметры пакета сервиса.");
                }

                if (request.Type == WindowsNamedPipeRequestTypes.InstallService)
                {
                    await serviceInstaller.InstallAsync(payload.Manifest, payload.PackageDirectory, cancellationToken);
                }
                else
                {
                    await serviceInstaller.UpdateAsync(payload.Manifest, payload.PackageDirectory, cancellationToken);
                }

                return Success();
            }

            case WindowsNamedPipeRequestTypes.UninstallService:
            {
                if (!await HasPolicyAsync(principal, ApiKitManagementAuthorizationDefaults.PackageManagementPolicyName))
                {
                    return Failure("forbidden", "Management peer не имеет прав удаления сервисов.");
                }

                var payload = WindowsNamedPipeMessageSerializer.FromElement<ManagedServiceNameRequest>(request.Payload);
                if (payload is null)
                {
                    return Failure("invalid_request", "Отсутствует имя Windows Service.");
                }

                await serviceInstaller.UninstallAsync(payload.ServiceName, cancellationToken);
                return Success();
            }

            default:
                return Failure("unsupported_request", "Тип административного management-запроса не поддерживается.");
        }
    }

    private async ValueTask<bool> HasPolicyAsync(
        System.Security.Claims.ClaimsPrincipal principal,
        string policyName)
    {
        var result = await authorizationService.AuthorizeAsync(principal, resource: null, policyName);
        return result.Succeeded;
    }

    private static WindowsNamedPipeResponse Success<T>(T payload) => new()
    {
        Succeeded = true,
        Payload = WindowsNamedPipeMessageSerializer.ToElement(payload)
    };

    private static WindowsNamedPipeResponse Success() => new()
    {
        Succeeded = true
    };

    private static WindowsNamedPipeResponse Failure(string code, string message) => new()
    {
        Succeeded = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}
