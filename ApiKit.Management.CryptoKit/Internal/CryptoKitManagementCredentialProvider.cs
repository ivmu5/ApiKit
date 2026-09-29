using System.Security.Cryptography;
using ApiKit.Management.Abstractions;
using ApiKit.Management.CryptoKit.Models;
using ApiKit.Management.CryptoKit.Options;
using ApiKit.Management.CryptoKit.Security;
using ApiKit.Management.Security;
using CryptoKit.Rsa;

namespace ApiKit.Management.CryptoKit.Internal;

internal sealed class CryptoKitManagementCredentialProvider(
    IRsaKeyProvider rsaKeyProvider,
    CryptoKitManagementOptionsSnapshot options,
    TimeProvider timeProvider)
    : IManagementCredentialProvider,
      ICryptoKitManagementPublicCredentialProvider
{
    private readonly IRsaKeyProvider _rsaKeyProvider = rsaKeyProvider
        ?? throw new ArgumentNullException(nameof(rsaKeyProvider));

    private readonly CryptoKitManagementOptionsSnapshot _options = options
        ?? throw new ArgumentNullException(nameof(options));

    private readonly TimeProvider _timeProvider = timeProvider
        ?? throw new ArgumentNullException(nameof(timeProvider));

    public ValueTask<string> GetCredentialIdAsync(
        ManagementPeerIdentity identity,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var credential = _options.GetActiveCredential(identity);
        return ValueTask.FromResult(credential.CredentialId);
    }

    public async ValueTask<ManagementChallengeResponse> CreateChallengeResponseAsync(
        ManagementPeerIdentity identity,
        ManagementChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(challenge);

        if (!ManagementPeerIdentitySecurityComparer.Equals(identity, challenge.Subject))
        {
            throw new InvalidOperationException(
                "Нельзя сформировать proof для management challenge, выпущенного для другой identity.");
        }

        var credential = _options.GetActiveCredential(identity);
        var createdAtUtc = _timeProvider.GetUtcNow();

        if (createdAtUtc < challenge.IssuedAtUtc - _options.ClockSkew ||
            createdAtUtc > challenge.ExpiresAtUtc + _options.ClockSkew)
        {
            throw new InvalidOperationException(
                "Нельзя формировать proof для management challenge вне допустимого временного окна.");
        }

        var payload = ManagementChallengeProofPayload.Create(
            challenge,
            identity,
            credential.CredentialId,
            createdAtUtc);

        using var keyPair = await GetKeyPairAsync(
                credential,
                cancellationToken)
            .ConfigureAwait(false);

        var privateKey = keyPair.ExportPrivateKey();

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKey, out var bytesRead);

            if (bytesRead != privateKey.Length)
            {
                throw new CryptographicException(
                    "Закрытый management RSA-ключ содержит лишние или некорректные данные.");
            }

            var proof = rsa.SignData(
                payload,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);

            return new ManagementChallengeResponse
            {
                ChallengeId = challenge.ChallengeId,
                Responder = identity,
                CredentialId = credential.CredentialId,
                Proof = proof,
                CreatedAtUtc = createdAtUtc
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    public async ValueTask<CryptoKitManagementPublicCredential> GetActiveAsync(
        ManagementPeerIdentity identity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var credential = _options.GetActiveCredential(identity);
        var publicKey = credential.CreateIfMissing
            ? await _rsaKeyProvider.GetOrCreatePublicKeyAsync(
                    credential.KeyId,
                    cancellationToken)
                .ConfigureAwait(false)
            : await _rsaKeyProvider.GetPublicKeyAsync(
                    credential.KeyId,
                    cancellationToken)
                .ConfigureAwait(false);

        return new CryptoKitManagementPublicCredential(
            identity,
            credential.CredentialId,
            publicKey);
    }

    private ValueTask<RsaKeyPair> GetKeyPairAsync(
        CryptoKitManagementCredentialRegistration credential,
        CancellationToken cancellationToken)
    {
        return credential.CreateIfMissing
            ? _rsaKeyProvider.GetOrCreateKeyPairAsync(
                credential.KeyId,
                cancellationToken)
            : _rsaKeyProvider.GetKeyPairAsync(
                credential.KeyId,
                cancellationToken);
    }
}
