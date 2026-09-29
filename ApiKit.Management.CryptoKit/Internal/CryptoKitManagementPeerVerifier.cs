using System.Security.Cryptography;
using ApiKit.Management.Abstractions;
using ApiKit.Management.CryptoKit.Security;
using ApiKit.Management.Models;
using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Internal;

internal sealed class CryptoKitManagementPeerVerifier(
    ICryptoKitManagementTrustStore trustStore,
    CryptoKitManagementOptionsSnapshot options,
    TimeProvider timeProvider) : IManagementPeerVerifier
{
    private readonly ICryptoKitManagementTrustStore _trustStore = trustStore
        ?? throw new ArgumentNullException(nameof(trustStore));

    private readonly CryptoKitManagementOptionsSnapshot _options = options
        ?? throw new ArgumentNullException(nameof(options));

    private readonly TimeProvider _timeProvider = timeProvider
        ?? throw new ArgumentNullException(nameof(timeProvider));

    public async ValueTask<ManagementAuthenticationResult> VerifyAsync(
        ManagementPeerContext transportPeer,
        ManagementChallenge challenge,
        ManagementChallengeResponse response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transportPeer);
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(response);

        if (!StringComparer.Ordinal.Equals(
                response.ChallengeId,
                challenge.ChallengeId))
        {
            return Failure("challenge_id", "Ответ относится к другому management challenge.");
        }

        if (response.Responder is null)
        {
            return Failure("identity_missing", "Challenge response не содержит responder identity.");
        }

        if (string.IsNullOrWhiteSpace(response.Responder.PeerId) ||
            response.Responder.Kind == ManagementPeerKind.Unknown ||
            !Enum.IsDefined(response.Responder.Kind))
        {
            return Failure("identity_invalid", "Challenge response содержит некорректную responder identity.");
        }

        if (!ManagementPeerIdentitySecurityComparer.Equals(
                challenge.Subject,
                response.Responder))
        {
            return Failure("identity_mismatch", "Responder не соответствует subject management challenge.");
        }

        if (!StringComparer.Ordinal.Equals(
                transportPeer.Transport,
                challenge.Transport))
        {
            return Failure("transport_mismatch", "Management challenge выпущен для другого transport.");
        }

        if (transportPeer.Properties.TryGetValue("purpose", out var transportPurpose) &&
            !StringComparer.Ordinal.Equals(transportPurpose, challenge.Purpose))
        {
            return Failure("purpose_mismatch", "Management challenge выпущен для другого назначения соединения.");
        }

        var now = _timeProvider.GetUtcNow();
        var clockSkew = _options.ClockSkew;

        if (now < challenge.IssuedAtUtc - clockSkew ||
            now > challenge.ExpiresAtUtc + clockSkew)
        {
            return Failure("challenge_expired", "Management challenge находится вне допустимого временного окна.");
        }

        if (response.CreatedAtUtc < challenge.IssuedAtUtc - clockSkew ||
            response.CreatedAtUtc > challenge.ExpiresAtUtc + clockSkew ||
            response.CreatedAtUtc > now + clockSkew)
        {
            return Failure("response_time", "Время формирования challenge response недопустимо.");
        }

        if (string.IsNullOrWhiteSpace(response.CredentialId))
        {
            return Failure("credential_missing", "Challenge response не содержит CredentialId.");
        }

        if (response.Proof is null || response.Proof.Length == 0)
        {
            return Failure("proof_missing", "Challenge response не содержит proof.");
        }

        try
        {
            var publicKey = await _trustStore.TryGetPublicKeyAsync(
                    response.Responder,
                    response.CredentialId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (publicKey is null)
            {
                return Failure("credential_untrusted", "Для указанного management credential отсутствует trusted public key.");
            }

            var payload = ManagementChallengeProofPayload.Create(
                challenge,
                response.Responder,
                response.CredentialId,
                response.CreatedAtUtc);

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKey, out var bytesRead);

            if (bytesRead != publicKey.Length)
            {
                return Failure("public_key_invalid", "Trusted public key имеет некорректный формат.");
            }

            var valid = rsa.VerifyData(
                payload,
                response.Proof,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);

            return valid
                ? new ManagementAuthenticationResult
                {
                    Succeeded = true,
                    Identity = response.Responder,
                    CredentialId = response.CredentialId
                }
                : Failure("proof_invalid", "Криптографический proof management peer не прошёл проверку.");
        }
        catch (CryptographicException)
        {
            return Failure("public_key_invalid", "Trusted public key имеет некорректный RSA-формат.");
        }
        catch (ArgumentException)
        {
            return Failure("response_invalid", "Challenge response содержит некорректные security-параметры.");
        }
    }

    private static ManagementAuthenticationResult Failure(
        string code,
        string reason)
    {
        return new ManagementAuthenticationResult
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason
        };
    }
}
