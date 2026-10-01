using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;
using ApiKit.Management.Security;
using ApiKit.Management.Windows.Options;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Windows.Internal;

internal sealed class WindowsNamedPipeManagementServicePublisher(
    IOptions<WindowsManagementServiceOptions> options,
    IManagementServiceIdentityProvider identityProvider,
    IManagementCredentialProvider credentialProvider,
    IManagementChallengeProvider challengeProvider,
    IManagementPeerVerifier peerVerifier)
    : IManagementServicePublisher
{
    public async ValueTask PublishAsync(
        ManagementServiceRegistration registration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        await SendAsync(
            WindowsNamedPipeRequestTypes.Register,
            registration,
            cancellationToken);
    }

    public async ValueTask WithdrawAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        await SendAsync(
            WindowsNamedPipeRequestTypes.Withdraw,
            new WithdrawRegistrationRequest(instanceId),
            cancellationToken);
    }

    private async ValueTask SendAsync<T>(
        string type,
        T payload,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        await using var pipe = await WindowsNamedPipeClientConnection.ConnectAsync(
            settings.RegistrationPipeName,
            settings.ConnectTimeout,
            cancellationToken);

        var serviceIdentity = ManagementPeerIdentity.ForManagedService(
            identityProvider.GetIdentity());
        var expectedHostIdentity = ManagementPeerIdentity.ForManagementHost(
            settings.ManagementHostPeerId);
        var transportPeer = WindowsNamedPipeAuthentication.CreateClientPeerContext(
            settings.RegistrationPipeName,
            WindowsManagementTransportDefaults.RegistrationPurpose);

        var hostAuthentication = await WindowsNamedPipeChallengeResponseProtocol.VerifyIdentityAsync(
            pipe,
            serviceIdentity,
            transportPeer,
            challengeProvider,
            peerVerifier,
            WindowsManagementTransportDefaults.RegistrationPurpose,
            settings.MaxMessageBytes,
            cancellationToken,
            identity => ManagementPeerIdentitySecurityComparer.Equals(identity, expectedHostIdentity));

        if (!hostAuthentication.Succeeded)
        {
            throw new InvalidOperationException(
                hostAuthentication.FailureReason
                ?? "Management host не прошёл криптографическую проверку identity.");
        }

        var serviceAuthentication = await WindowsNamedPipeChallengeResponseProtocol.ProveIdentityAsync(
            pipe,
            serviceIdentity,
            credentialProvider,
            WindowsManagementTransportDefaults.RegistrationPurpose,
            settings.MaxMessageBytes,
            cancellationToken,
            expectedChallengeIssuer: expectedHostIdentity);

        if (!serviceAuthentication.Succeeded)
        {
            throw new InvalidOperationException(
                serviceAuthentication.FailureReason
                ?? "Management host отклонил криптографическую identity сервиса.");
        }

        await WindowsNamedPipeMessageSerializer.WriteAsync(
            pipe,
            new WindowsNamedPipeRequest
            {
                Type = type,
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
            throw new InvalidOperationException(
                "Management host использует несовместимую версию transport protocol.");
        }

        if (!response.Succeeded)
        {
            throw new InvalidOperationException(
                response.ErrorMessage ?? "Management host отклонил регистрацию сервиса.");
        }
    }
}
