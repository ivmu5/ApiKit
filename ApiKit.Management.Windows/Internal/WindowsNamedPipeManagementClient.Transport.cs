using ApiKit.Management.Security;

namespace ApiKit.Management.Windows.Internal;

internal sealed partial class WindowsNamedPipeManagementClient
{
    private ValueTask<WindowsNamedPipeResponse> SendAsync(
        string type,
        CancellationToken cancellationToken) =>
        SendAsync<object?>(type, null, cancellationToken);

    private async ValueTask<WindowsNamedPipeResponse> SendAsync<T>(
        string type,
        T? payload,
        CancellationToken cancellationToken)
    {
        var settings = _options.Value;
        await using var pipe = await WindowsNamedPipeClientConnection.ConnectAsync(
            settings.AdministrationPipeName,
            settings.ConnectTimeout,
            cancellationToken);

        var adminIdentity = ManagementPeerIdentity.ForAdminClient(
            settings.AdminClientPeerId);
        var hostIdentity = ManagementPeerIdentity.ForManagementHost(
            settings.ManagementHostPeerId);
        var transportPeer = WindowsNamedPipeAuthentication.CreateClientPeerContext(
            settings.AdministrationPipeName,
            WindowsManagementTransportDefaults.AdministrationPurpose);

        // Host доказывает identity первым. Админ-панель не раскрывает собственный proof
        // и не отправляет административный payload неизвестному локальному pipe.
        var hostAuthentication = await WindowsNamedPipeChallengeResponseProtocol.VerifyIdentityAsync(
            pipe,
            adminIdentity,
            transportPeer,
            _challengeProvider,
            _peerVerifier,
            WindowsManagementTransportDefaults.AdministrationPurpose,
            settings.MaxMessageBytes,
            cancellationToken,
            identity => ManagementPeerIdentitySecurityComparer.Equals(identity, hostIdentity));

        if (!hostAuthentication.Succeeded ||
            hostAuthentication.Identity is null ||
            string.IsNullOrWhiteSpace(hostAuthentication.CredentialId))
        {
            throw CreateAuthenticationException(
                hostAuthentication,
                "Management host не прошёл криптографическую аутентификацию.");
        }

        var adminAuthentication = await WindowsNamedPipeChallengeResponseProtocol.ProveIdentityAsync(
            pipe,
            adminIdentity,
            _credentialProvider,
            WindowsManagementTransportDefaults.AdministrationPurpose,
            settings.MaxMessageBytes,
            cancellationToken,
            expectedChallengeIssuer: hostIdentity);

        if (!adminAuthentication.Succeeded)
        {
            throw CreateAuthenticationException(
                adminAuthentication,
                "Management host отклонил credential административного клиента.");
        }

        await WindowsNamedPipeMessageSerializer.WriteAsync(
            pipe,
            new WindowsNamedPipeRequest
            {
                Type = type,
                Payload = payload is null
                    ? null
                    : WindowsNamedPipeMessageSerializer.ToElement(payload)
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
                $"{response.ErrorCode ?? "management_error"}: " +
                (response.ErrorMessage ?? "Management host отклонил запрос."));
        }

        return response;
    }

    private static InvalidOperationException CreateAuthenticationException(
        ManagementAuthenticationResult result,
        string fallbackMessage)
    {
        var code = result.FailureCode ?? "authentication_failed";
        var message = result.FailureReason ?? fallbackMessage;
        return new InvalidOperationException($"{code}: {message}");
    }

    private static T? ReadPayload<T>(WindowsNamedPipeResponse response) =>
        WindowsNamedPipeMessageSerializer.FromElement<T>(response.Payload);
}
