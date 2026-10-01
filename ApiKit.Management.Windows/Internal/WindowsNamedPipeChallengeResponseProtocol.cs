using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;
using ApiKit.Management.Security;

namespace ApiKit.Management.Windows.Internal;

/// <summary>
/// Implements mutual challenge-response authentication over the length-prefixed Named Pipe transport.
/// </summary>
internal static class WindowsNamedPipeChallengeResponseProtocol
{
    public static async ValueTask<ManagementAuthenticationResult> ProveIdentityAsync(
        Stream stream,
        ManagementPeerIdentity localIdentity,
        IManagementCredentialProvider credentialProvider,
        string purpose,
        int maxMessageBytes,
        CancellationToken cancellationToken,
        ManagementPeerIdentity? expectedChallengeIssuer = null,
        Func<ManagementPeerIdentity, bool>? challengeIssuerValidator = null,
        TimeSpan? maximumHandshakeDuration = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(localIdentity);
        ArgumentNullException.ThrowIfNull(credentialProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        // Bound every challenge-response round trip to avoid retaining open
        // Named Pipe instances indefinitely for stalled or hostile peers.
        if (maximumHandshakeDuration is { } duration && duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumHandshakeDuration));
        }

        using var handshakeTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        handshakeTimeout.CancelAfter(maximumHandshakeDuration ?? TimeSpan.FromSeconds(30));
        var handshakeToken = handshakeTimeout.Token;

        ValidateIdentity(localIdentity, nameof(localIdentity));

        await WindowsNamedPipeMessageSerializer.WriteAsync(
            stream,
            new WindowsNamedPipeSecurityHello
            {
                Identity = localIdentity
            },
            maxMessageBytes,
            handshakeToken);

        var challengeFrame = await WindowsNamedPipeMessageSerializer.ReadAsync<WindowsNamedPipeSecurityChallenge>(
            stream,
            maxMessageBytes,
            handshakeToken);

        EnsureProtocolVersion(challengeFrame.ProtocolVersion);

        if (challengeFrame.Challenge is null)
        {
            throw new InvalidDataException(
                "Challenge-response сообщение не содержит management challenge.");
        }

        ValidateChallengeForProver(
            challengeFrame.Challenge,
            localIdentity,
            purpose,
            expectedChallengeIssuer,
            challengeIssuerValidator);

        var response = await credentialProvider.CreateChallengeResponseAsync(
            localIdentity,
            challengeFrame.Challenge,
            handshakeToken);

        await WindowsNamedPipeMessageSerializer.WriteAsync(
            stream,
            new WindowsNamedPipeSecurityResponse
            {
                Response = response
            },
            maxMessageBytes,
            handshakeToken);

        var resultFrame = await WindowsNamedPipeMessageSerializer.ReadAsync<WindowsNamedPipeSecurityResult>(
            stream,
            maxMessageBytes,
            handshakeToken);

        EnsureProtocolVersion(resultFrame.ProtocolVersion);

        if (!resultFrame.Succeeded)
        {
            return Failure(
                resultFrame.ErrorCode ?? "authentication_failed",
                resultFrame.ErrorMessage ?? "Management peer отклонил challenge response.");
        }

        if (!ManagementPeerIdentitySecurityComparer.Equals(
                resultFrame.Identity,
                localIdentity))
        {
            return Failure(
                "result_identity",
                "Результат challenge-response относится к другой management identity.");
        }

        if (!StringComparer.Ordinal.Equals(
                resultFrame.CredentialId,
                response.CredentialId))
        {
            return Failure(
                "result_credential",
                "Результат challenge-response относится к другому credential.");
        }

        return new ManagementAuthenticationResult
        {
            Succeeded = true,
            Identity = resultFrame.Identity,
            CredentialId = resultFrame.CredentialId
        };
    }

    public static async ValueTask<ManagementAuthenticationResult> VerifyIdentityAsync(
        Stream stream,
        ManagementPeerIdentity verifierIdentity,
        ManagementPeerContext transportPeer,
        IManagementChallengeProvider challengeProvider,
        IManagementPeerVerifier peerVerifier,
        string purpose,
        int maxMessageBytes,
        CancellationToken cancellationToken,
        Func<ManagementPeerIdentity, bool>? identityValidator = null,
        TimeSpan? maximumHandshakeDuration = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(verifierIdentity);
        ArgumentNullException.ThrowIfNull(transportPeer);
        ArgumentNullException.ThrowIfNull(challengeProvider);
        ArgumentNullException.ThrowIfNull(peerVerifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        // Bound every challenge-response round trip to avoid retaining open
        // Named Pipe instances indefinitely for stalled or hostile peers.
        if (maximumHandshakeDuration is { } duration && duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumHandshakeDuration));
        }

        using var handshakeTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        handshakeTimeout.CancelAfter(maximumHandshakeDuration ?? TimeSpan.FromSeconds(30));
        var handshakeToken = handshakeTimeout.Token;

        ValidateIdentity(verifierIdentity, nameof(verifierIdentity));

        var hello = await WindowsNamedPipeMessageSerializer.ReadAsync<WindowsNamedPipeSecurityHello>(
            stream,
            maxMessageBytes,
            handshakeToken);

        if (hello.ProtocolVersion != WindowsNamedPipeProtocol.Version)
        {
            var versionFailure = Failure(
                "protocol_version",
                "Версия challenge-response protocol не поддерживается.");

            await WriteResultAsync(stream, versionFailure, maxMessageBytes, handshakeToken);
            return versionFailure;
        }

        try
        {
            ValidateIdentity(hello.Identity, nameof(hello.Identity));
        }
        catch (ArgumentException exception)
        {
            var identityFailure = Failure("identity_invalid", exception.Message);
            await WriteResultAsync(stream, identityFailure, maxMessageBytes, handshakeToken);
            return identityFailure;
        }

        if (identityValidator is not null && !identityValidator(hello.Identity))
        {
            var identityFailure = Failure(
                "identity_unexpected",
                "Management peer предъявил identity, недопустимую для этого соединения.");

            await WriteResultAsync(stream, identityFailure, maxMessageBytes, handshakeToken);
            return identityFailure;
        }

        ManagementChallenge? challenge = null;
        var challengeConsumed = false;

        try
        {
            challenge = await challengeProvider.CreateChallengeAsync(
                verifierIdentity,
                hello.Identity,
                WindowsManagementTransportDefaults.TransportName,
                purpose,
                WindowsNamedPipeProtocol.Version,
                handshakeToken);

            await WindowsNamedPipeMessageSerializer.WriteAsync(
                stream,
                new WindowsNamedPipeSecurityChallenge
                {
                    Challenge = challenge
                },
                maxMessageBytes,
                handshakeToken);

            var responseFrame = await WindowsNamedPipeMessageSerializer.ReadAsync<WindowsNamedPipeSecurityResponse>(
                stream,
                maxMessageBytes,
                handshakeToken);

            if (responseFrame.ProtocolVersion != WindowsNamedPipeProtocol.Version)
            {
                var versionFailure = Failure(
                    "protocol_version",
                    "Версия challenge-response protocol не поддерживается.");

                await WriteResultAsync(stream, versionFailure, maxMessageBytes, handshakeToken);
                return versionFailure;
            }

            if (responseFrame.Response is null)
            {
                var responseFailure = Failure(
                    "response_missing",
                    "Challenge-response сообщение не содержит proof.");

                await WriteResultAsync(stream, responseFailure, maxMessageBytes, handshakeToken);
                return responseFailure;
            }

            var consumedChallenge = await challengeProvider.ConsumeChallengeAsync(
                challenge.ChallengeId,
                handshakeToken);
            challengeConsumed = true;

            if (consumedChallenge is null)
            {
                var challengeFailure = Failure(
                    "challenge_invalid",
                    "Management challenge неизвестен, истёк или уже был использован.");

                await WriteResultAsync(stream, challengeFailure, maxMessageBytes, handshakeToken);
                return challengeFailure;
            }

            var result = await peerVerifier.VerifyAsync(
                transportPeer,
                consumedChallenge,
                responseFrame.Response,
                handshakeToken);

            await WriteResultAsync(stream, result, maxMessageBytes, handshakeToken);
            return result;
        }
        finally
        {
            if (challenge is not null && !challengeConsumed)
            {
                try
                {
                    await challengeProvider.ConsumeChallengeAsync(
                        challenge.ChallengeId,
                        CancellationToken.None);
                }
                catch
                {
                }
            }
        }
    }

    private static async ValueTask WriteResultAsync(
        Stream stream,
        ManagementAuthenticationResult result,
        int maxMessageBytes,
        CancellationToken cancellationToken)
    {
        await WindowsNamedPipeMessageSerializer.WriteAsync(
            stream,
            new WindowsNamedPipeSecurityResult
            {
                Succeeded = result.Succeeded,
                Identity = result.Identity,
                CredentialId = result.CredentialId,
                ErrorCode = result.FailureCode,
                ErrorMessage = result.FailureReason
            },
            maxMessageBytes,
            cancellationToken);
    }

    private static void ValidateChallengeForProver(
        ManagementChallenge challenge,
        ManagementPeerIdentity localIdentity,
        string purpose,
        ManagementPeerIdentity? expectedIssuer,
        Func<ManagementPeerIdentity, bool>? issuerValidator)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        if (challenge.ProtocolVersion != WindowsNamedPipeProtocol.Version)
        {
            throw new InvalidDataException(
                "Challenge выпущен для несовместимой версии management protocol.");
        }

        if (!StringComparer.Ordinal.Equals(
                challenge.Transport,
                WindowsManagementTransportDefaults.TransportName))
        {
            throw new InvalidDataException(
                "Challenge выпущен для другого management transport.");
        }

        if (!StringComparer.Ordinal.Equals(challenge.Purpose, purpose))
        {
            throw new InvalidDataException(
                "Challenge выпущен для другого назначения management соединения.");
        }

        if (!ManagementPeerIdentitySecurityComparer.Equals(
                challenge.Subject,
                localIdentity))
        {
            throw new InvalidDataException(
                "Challenge выпущен для другой management identity.");
        }

        if (expectedIssuer is not null &&
            !ManagementPeerIdentitySecurityComparer.Equals(
                challenge.Issuer,
                expectedIssuer))
        {
            throw new InvalidDataException(
                "Challenge выпущен неожиданной management identity.");
        }

        if (issuerValidator is not null && !issuerValidator(challenge.Issuer))
        {
            throw new InvalidDataException(
                "Challenge выпущен identity, недопустимой для этого management соединения.");
        }
    }

    private static void ValidateIdentity(
        ManagementPeerIdentity identity,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(identity, parameterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.PeerId, parameterName);

        if (identity.Kind == ManagementPeerKind.Unknown ||
            !Enum.IsDefined(identity.Kind))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Неизвестный тип management peer.");
        }
    }

    private static void EnsureProtocolVersion(int protocolVersion)
    {
        if (protocolVersion != WindowsNamedPipeProtocol.Version)
        {
            throw new InvalidDataException(
                "Management peer использует несовместимую версию challenge-response protocol.");
        }
    }

    private static ManagementAuthenticationResult Failure(
        string code,
        string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason
        };
}
