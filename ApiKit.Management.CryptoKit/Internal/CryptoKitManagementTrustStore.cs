using System.Security.Cryptography;
using ApiKit.Management.CryptoKit.Security;
using ApiKit.Management.Security;
using CryptoKit.Storage;

namespace ApiKit.Management.CryptoKit.Internal;

internal sealed class CryptoKitManagementTrustStore(
    IKeyStorage storage) : ICryptoKitManagementTrustStore
{
    private readonly IKeyStorage _storage = storage
        ?? throw new ArgumentNullException(nameof(storage));

    public async ValueTask TrustAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        ReadOnlyMemory<byte> publicKeySubjectPublicKeyInfo,
        CancellationToken cancellationToken = default)
    {
        ValidatePublicKey(publicKeySubjectPublicKeyInfo.Span);

        var storageKeyId = CryptoKitManagementTrustKeyId.Create(
            identity,
            credentialId);

        if (await _storage.CreateAsync(
                storageKeyId,
                publicKeySubjectPublicKeyInfo,
                cancellationToken)
            .ConfigureAwait(false))
        {
            return;
        }

        var existing = await _storage.TryLoadAsync(
                storageKeyId,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            throw new InvalidOperationException(
                "Trusted public key исчез между проверкой существования и чтением.");
        }

        if (!CryptographicOperations.FixedTimeEquals(
                existing,
                publicKeySubjectPublicKeyInfo.Span))
        {
            throw new CryptographicException(
                $"Credential '{credentialId}' уже связан с другим trusted public key. " +
                "Для ротации используйте новый CredentialId.");
        }
    }

    public ValueTask RevokeAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default)
    {
        var storageKeyId = CryptoKitManagementTrustKeyId.Create(
            identity,
            credentialId);

        return _storage.DeleteAsync(storageKeyId, cancellationToken);
    }

    public async ValueTask<byte[]?> TryGetPublicKeyAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default)
    {
        var storageKeyId = CryptoKitManagementTrustKeyId.Create(
            identity,
            credentialId);

        var publicKey = await _storage.TryLoadAsync(
                storageKeyId,
                cancellationToken)
            .ConfigureAwait(false);

        if (publicKey is null)
        {
            return null;
        }

        ValidatePublicKey(publicKey);
        return publicKey;
    }

    private static void ValidatePublicKey(ReadOnlySpan<byte> publicKey)
    {
        if (publicKey.IsEmpty)
        {
            throw new ArgumentException(
                "Trusted public key не может быть пустым.",
                nameof(publicKey));
        }

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKey, out var bytesRead);

        if (bytesRead != publicKey.Length)
        {
            throw new CryptographicException(
                "Trusted public key содержит лишние или некорректные данные.");
        }

        if (rsa.KeySize < 2048)
        {
            throw new CryptographicException(
                "Размер trusted RSA public key должен быть не меньше 2048 бит.");
        }
    }
}
