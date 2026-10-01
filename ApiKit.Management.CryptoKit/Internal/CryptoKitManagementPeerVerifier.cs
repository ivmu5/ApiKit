using System.Security.Cryptography;
using ApiKit.Management.Abstractions;
using ApiKit.Management.CryptoKit.Models;
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

        if (!transportPeer.Properties.TryGetValue("purpose", out var transportPurpose) ||
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
            var trustedCredential = await _trustStore.TryGetAsync(
                    response.Responder,
                    response.CredentialId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (trustedCredential is null)
            {
                return Failure("credential_untrusted", "Для указанного management credential отсутствует trusted public key.");
            }

            if (trustedCredential.Status == CryptoKitManagementTrustedCredentialStatus.Retiring)
            {
                if (trustedCredential.AcceptUntilUtc is null)
                {
                    return Failure("credential_state", "Retiring trusted credential не содержит срока overlap.");
                }

                if (now > trustedCredential.AcceptUntilUtc.Value + clockSkew)
                {
                    return Failure("credential_retired", "Overlap-период retiring management credential завершён.");
                }
            }

            var payload = ManagementChallengeProofPayload.Create(
                challenge,
                response.Responder,
                response.CredentialId,
                response.CreatedAtUtc);

            var valid = CryptoKitManagementRsaProof.Verify(
                trustedCredential.PublicKeySubjectPublicKeyInfo,
                payload,
                response.Proof);

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
            return Failure("public_key_invalid", "Trusted public key или trust record имеет некорректный криптографический формат.");
        }
        catch (ArgumentException)
        {
            return Failure("response_invalid", "Challenge response содержит некорректные security-параметры.");
        }
        catch (InvalidOperationException)
        {
            return Failure("credential_state", "Trusted credential state имеет некорректный формат или состояние.");
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
