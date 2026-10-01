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
    CryptoKitManagementCredentialRegistry registry,
    CryptoKitManagementOptionsSnapshot options,
    TimeProvider timeProvider)
    : IManagementCredentialProvider,
      ICryptoKitManagementPublicCredentialProvider
{
    private readonly IRsaKeyProvider _rsaKeyProvider = rsaKeyProvider
        ?? throw new ArgumentNullException(nameof(rsaKeyProvider));

    private readonly CryptoKitManagementCredentialRegistry _registry = registry
        ?? throw new ArgumentNullException(nameof(registry));

    private readonly CryptoKitManagementOptionsSnapshot _options = options
        ?? throw new ArgumentNullException(nameof(options));

    private readonly TimeProvider _timeProvider = timeProvider
        ?? throw new ArgumentNullException(nameof(timeProvider));

    public async ValueTask<string> GetCredentialIdAsync(
        ManagementPeerIdentity identity,
        CancellationToken cancellationToken = default)
    {
        var credential = await _registry.GetActiveCredentialAsync(
                identity,
                cancellationToken)
            .ConfigureAwait(false);

        return credential.CredentialId;
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

        var credential = await _registry.GetActiveCredentialAsync(
                identity,
                cancellationToken)
            .ConfigureAwait(false);

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
            var proof = CryptoKitManagementRsaProof.Sign(
                privateKey,
                payload);

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

        var credential = await _registry.GetActiveCredentialAsync(
                identity,
                cancellationToken)
            .ConfigureAwait(false);

        return await ExportPublicCredentialAsync(
                identity,
                credential,
                CryptoKitManagementLocalCredentialStatus.Active,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<CryptoKitManagementPublicCredential> GetAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);

        var (credential, status) = await _registry.GetCredentialAsync(
                identity,
                credentialId,
                cancellationToken)
            .ConfigureAwait(false);

        return await ExportPublicCredentialAsync(
                identity,
                credential,
                status,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<CryptoKitManagementPublicCredential>> GetAllAsync(
        ManagementPeerIdentity identity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var credentials = await _registry.GetCredentialsAsync(
                identity,
                cancellationToken)
            .ConfigureAwait(false);

        var result = new List<CryptoKitManagementPublicCredential>(credentials.Count);

        foreach (var credential in credentials)
        {
            result.Add(await GetAsync(
                    identity,
                    credential.CredentialId,
                    cancellationToken)
                .ConfigureAwait(false));
        }

        return result;
    }

    private async ValueTask<CryptoKitManagementPublicCredential> ExportPublicCredentialAsync(
        ManagementPeerIdentity identity,
        CryptoKitManagementCredentialRegistration credential,
        CryptoKitManagementLocalCredentialStatus status,
        CancellationToken cancellationToken)
    {
        var canCreate = credential.CreateIfMissing &&
                        status != CryptoKitManagementLocalCredentialStatus.Retired;

        var publicKey = canCreate
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
            publicKey,
            status);
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
