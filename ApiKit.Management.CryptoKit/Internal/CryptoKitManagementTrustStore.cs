using System.Security.Cryptography;
using System.Text.Json;
using ApiKit.Management.CryptoKit.Models;
using ApiKit.Management.CryptoKit.Security;
using ApiKit.Management.Security;
using CryptoKit.Storage;

namespace ApiKit.Management.CryptoKit.Internal;

internal sealed class CryptoKitManagementTrustStore(
    IKeyStorage storage,
    TimeProvider timeProvider) : ICryptoKitManagementTrustStore
{
    private const int RecordFormatVersion = 1;

    private readonly IKeyStorage _storage = storage
        ?? throw new ArgumentNullException(nameof(storage));

    private readonly TimeProvider _timeProvider = timeProvider
        ?? throw new ArgumentNullException(nameof(timeProvider));

    public async ValueTask TrustAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        ReadOnlyMemory<byte> publicKeySubjectPublicKeyInfo,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentity(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);
        CryptoKitManagementRsaProof.ValidatePublicKey(publicKeySubjectPublicKeyInfo.Span);

        var storageKeyId = CryptoKitManagementTrustKeyId.Create(identity, credentialId);
        var existing = await LoadAsync(
                storageKeyId,
                identity,
                credentialId,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var record = new StoredTrustRecord
            {
                Version = RecordFormatVersion,
                PeerId = identity.PeerId,
                Kind = identity.Kind,
                CredentialId = credentialId,
                PublicKeySubjectPublicKeyInfo = publicKeySubjectPublicKeyInfo.ToArray(),
                Status = CryptoKitManagementTrustedCredentialStatus.Trusted,
                TrustedAtUtc = _timeProvider.GetUtcNow()
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(record);
            if (await _storage.CreateAsync(storageKeyId, bytes, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            existing = await LoadAsync(
                    storageKeyId,
                    identity,
                    credentialId,
                    cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    "Trusted credential исчез после конкурентного создания.");
        }

        if (!CryptographicOperations.FixedTimeEquals(
                existing.PublicKeySubjectPublicKeyInfo,
                publicKeySubjectPublicKeyInfo.Span))
        {
            throw new CryptographicException(
                $"Credential '{credentialId}' уже связан с другим trusted public key. " +
                "Для ротации используйте новый CredentialId.");
        }

        if (existing.Status == CryptoKitManagementTrustedCredentialStatus.Trusted &&
            existing.AcceptUntilUtc is null)
        {
            return;
        }

        existing.Status = CryptoKitManagementTrustedCredentialStatus.Trusted;
        existing.AcceptUntilUtc = null;
        await SaveExistingAsync(storageKeyId, existing, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask MarkRetiringAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        DateTimeOffset acceptUntilUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentity(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);

        var now = _timeProvider.GetUtcNow();
        if (acceptUntilUtc <= now)
        {
            throw new ArgumentOutOfRangeException(
                nameof(acceptUntilUtc),
                "Retiring credential должен иметь будущий конец overlap-периода.");
        }

        var storageKeyId = CryptoKitManagementTrustKeyId.Create(identity, credentialId);
        var existing = await LoadAsync(
                storageKeyId,
                identity,
                credentialId,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException(
                $"Trusted management credential '{credentialId}' не найден.");

        existing.Status = CryptoKitManagementTrustedCredentialStatus.Retiring;
        existing.AcceptUntilUtc = acceptUntilUtc;
        await SaveExistingAsync(storageKeyId, existing, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask RevokeAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentity(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);

        var storageKeyId = CryptoKitManagementTrustKeyId.Create(identity, credentialId);
        return _storage.DeleteAsync(storageKeyId, cancellationToken);
    }

    public async ValueTask<CryptoKitManagementTrustedCredential?> TryGetAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentity(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);

        var storageKeyId = CryptoKitManagementTrustKeyId.Create(identity, credentialId);
        var record = await LoadAsync(
                storageKeyId,
                identity,
                credentialId,
                cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return null;
        }

        CryptoKitManagementRsaProof.ValidatePublicKey(record.PublicKeySubjectPublicKeyInfo);

        return new CryptoKitManagementTrustedCredential(
            identity,
            credentialId,
            record.PublicKeySubjectPublicKeyInfo.ToArray(),
            record.Status,
            record.TrustedAtUtc,
            record.AcceptUntilUtc);
    }

    private async ValueTask<StoredTrustRecord?> LoadAsync(
        string storageKeyId,
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken)
    {
        var bytes = await _storage.TryLoadAsync(storageKeyId, cancellationToken).ConfigureAwait(false);
        if (bytes is null)
        {
            return null;
        }

        StoredTrustRecord record;
        try
        {
            record = JsonSerializer.Deserialize<StoredTrustRecord>(bytes)
                ?? throw new InvalidOperationException("Trusted credential record пуст.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Trusted credential record имеет некорректный формат.",
                exception);
        }

        ValidateStoredRecord(record, identity, credentialId);
        return record;
    }

    private async ValueTask SaveExistingAsync(
        string storageKeyId,
        StoredTrustRecord record,
        CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(record);
        await _storage.ReplaceAsync(storageKeyId, bytes, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateStoredRecord(
        StoredTrustRecord record,
        ManagementPeerIdentity identity,
        string credentialId)
    {
        if (record.Version != RecordFormatVersion)
        {
            throw new InvalidOperationException(
                $"Неподдерживаемая версия trusted credential record: {record.Version}.");
        }

        if (!StringComparer.Ordinal.Equals(record.PeerId, identity.PeerId) ||
            record.Kind != identity.Kind ||
            !StringComparer.Ordinal.Equals(record.CredentialId, credentialId))
        {
            throw new CryptographicException(
                "Trusted credential record не соответствует запрошенной management identity.");
        }

        if (record.PublicKeySubjectPublicKeyInfo is null ||
            record.PublicKeySubjectPublicKeyInfo.Length == 0)
        {
            throw new CryptographicException("Trusted credential record не содержит public key.");
        }

        if (!Enum.IsDefined(record.Status))
        {
            throw new CryptographicException("Trusted credential record содержит неизвестный status.");
        }

        if (record.Status == CryptoKitManagementTrustedCredentialStatus.Retiring &&
            record.AcceptUntilUtc is null)
        {
            throw new CryptographicException(
                "Retiring trusted credential не содержит AcceptUntilUtc.");
        }

        if (record.Status == CryptoKitManagementTrustedCredentialStatus.Trusted &&
            record.AcceptUntilUtc is not null)
        {
            throw new CryptographicException(
                "Trusted credential содержит неожиданный AcceptUntilUtc.");
        }
    }

    private static void ValidateIdentity(ManagementPeerIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.PeerId);

        if (identity.Kind == ManagementPeerKind.Unknown || !Enum.IsDefined(identity.Kind))
        {
            throw new ArgumentOutOfRangeException(nameof(identity), "Management identity имеет неизвестный тип peer.");
        }
    }

    public sealed class StoredTrustRecord
    {
        public int Version { get; set; }

        public string PeerId { get; set; } = string.Empty;

        public ManagementPeerKind Kind { get; set; }

        public string CredentialId { get; set; } = string.Empty;

        public byte[] PublicKeySubjectPublicKeyInfo { get; set; } = [];

        public CryptoKitManagementTrustedCredentialStatus Status { get; set; }

        public DateTimeOffset TrustedAtUtc { get; set; }

        public DateTimeOffset? AcceptUntilUtc { get; set; }
    }
}
