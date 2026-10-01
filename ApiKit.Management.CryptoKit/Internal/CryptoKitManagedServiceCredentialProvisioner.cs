using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApiKit.Management.Abstractions;
using ApiKit.Management.CryptoKit.Models;
using ApiKit.Management.CryptoKit.Options;
using ApiKit.Management.CryptoKit.Provisioning;
using ApiKit.Management.CryptoKit.Security;
using ApiKit.Management.Provisioning;
using ApiKit.Management.Security;
using CryptoKit.Rsa;
using CryptoKit.Storage;

namespace ApiKit.Management.CryptoKit.Internal;

/// <summary>
/// Creates isolated CryptoKit key storage and establishes mutual public-key trust between a new service and its management host.
/// </summary>
internal sealed class CryptoKitManagedServiceCredentialProvisioner(
    IKeyStorage hostStorage,
    ICryptoKitManagementPublicCredentialProvider hostPublicCredentials,
    ICryptoKitManagementTrustStore hostTrustStore,
    CryptoKitManagedServiceProvisioningOptionsSnapshot options,
    TimeProvider timeProvider)
    : IManagedServiceCredentialProvisioner
{
    private const string ProvisioningRecordPrefix = "apikit.management.provisioned-service.v1.";
    private const int ProvisioningRecordVersion = 1;
    private static readonly Encoding StrictUtf8 =
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly IKeyStorage _hostStorage = hostStorage
        ?? throw new ArgumentNullException(nameof(hostStorage));

    private readonly ICryptoKitManagementPublicCredentialProvider _hostPublicCredentials = hostPublicCredentials
        ?? throw new ArgumentNullException(nameof(hostPublicCredentials));

    private readonly ICryptoKitManagementTrustStore _hostTrustStore = hostTrustStore
        ?? throw new ArgumentNullException(nameof(hostTrustStore));

    private readonly CryptoKitManagedServiceProvisioningOptionsSnapshot _options = options
        ?? throw new ArgumentNullException(nameof(options));

    private readonly TimeProvider _timeProvider = timeProvider
        ?? throw new ArgumentNullException(nameof(timeProvider));

    public async ValueTask ProvisionAsync(
        ManagedServiceCredentialProvisioningContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ValidateContext(context);

        var existingProvisioningRecord = await TryLoadProvisioningRecordAsync(
                context.Manifest.ServiceName,
                cancellationToken)
            .ConfigureAwait(false);

        if (existingProvisioningRecord is not null
            && !StringComparer.Ordinal.Equals(
                existingProvisioningRecord.ServiceName,
                context.Manifest.ServiceName))
        {
            throw new InvalidOperationException(
                $"Windows service name '{context.Manifest.ServiceName}' отличается регистром от уже provisioned " +
                $"management identity '{existingProvisioningRecord.ServiceName}'. Сначала выполните deprovision старой identity.");
        }

        var serviceIdentity = new ManagementPeerIdentity(
            context.Manifest.ServiceName,
            ManagementPeerKind.ManagedService);

        var storageDirectory = CryptoKitManagementProvisioningPaths.GetStorageDirectory(
            context.ServiceRootDirectory,
            _options.CryptoKitDirectoryName);

        if (existingProvisioningRecord is not null && !Directory.Exists(storageDirectory))
        {
            throw new InvalidOperationException(
                $"Для сервиса '{context.Manifest.ServiceName}' существует Host provisioning record, " +
                "но локальный service CryptoKit storage отсутствует. Сначала выполните deprovision orphan state.");
        }

        CryptoKitManagementProvisioningPaths.EnsureSafeProvisioningDirectory(
            context.ServiceRootDirectory,
            storageDirectory);

        var serviceStorage = CreateServiceStorage(storageDirectory);
        var serviceRsaProvider = CreateServiceRsaProvider(serviceStorage);
        var serviceRegistry = CreateServiceRegistry(serviceStorage, serviceRsaProvider);
        var serviceTrustStore = new CryptoKitManagementTrustStore(serviceStorage, _timeProvider);

        var hostCredential = await _hostPublicCredentials
            .GetActiveAsync(context.ManagementHostIdentity, cancellationToken)
            .ConfigureAwait(false);

        var serviceCredential = await EnsureActiveServiceCredentialAsync(
                serviceIdentity,
                serviceRegistry,
                serviceRsaProvider,
                cancellationToken)
            .ConfigureAwait(false);

        await serviceTrustStore
            .TrustAsync(
                context.ManagementHostIdentity,
                hostCredential.CredentialId,
                hostCredential.PublicKeySubjectPublicKeyInfo,
                cancellationToken)
            .ConfigureAwait(false);

        await _hostTrustStore
            .TrustAsync(
                serviceIdentity,
                serviceCredential.CredentialId,
                serviceCredential.PublicKeySubjectPublicKeyInfo,
                cancellationToken)
            .ConfigureAwait(false);

        await SaveProvisioningRecordAsync(
                context.Manifest.ServiceName,
                serviceCredential.CredentialId,
                cancellationToken)
            .ConfigureAwait(false);

        CryptoKitManagementProvisioningPaths.EnsureSafeProvisioningDirectory(
            context.ServiceRootDirectory,
            storageDirectory);
    }

    public async ValueTask DeprovisionAsync(
        ManagedServiceCredentialDeprovisioningContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.ServiceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.ServiceRootDirectory);
        ArgumentNullException.ThrowIfNull(context.ManagementHostIdentity);

        var credentialIds = new HashSet<string>(StringComparer.Ordinal);
        var provisioningRecord = await TryLoadProvisioningRecordAsync(
                context.ServiceName,
                cancellationToken)
            .ConfigureAwait(false);

        var canonicalServiceName = provisioningRecord?.ServiceName ?? context.ServiceName;
        var serviceIdentity = new ManagementPeerIdentity(
            canonicalServiceName,
            ManagementPeerKind.ManagedService);

        if (provisioningRecord is not null)
        {
            foreach (var credentialId in provisioningRecord.CredentialIds)
            {
                if (!string.IsNullOrWhiteSpace(credentialId))
                {
                    credentialIds.Add(credentialId);
                }
            }
        }

        var storageDirectory = CryptoKitManagementProvisioningPaths.GetStorageDirectory(
            context.ServiceRootDirectory,
            _options.CryptoKitDirectoryName);

        Exception? localStateError = null;

        if (Directory.Exists(storageDirectory))
        {
            try
            {
                CryptoKitManagementProvisioningPaths.EnsureSafeProvisioningDirectory(
                    context.ServiceRootDirectory,
                    storageDirectory);

                var serviceStorage = CreateServiceStorage(storageDirectory);
                var serviceRsaProvider = CreateServiceRsaProvider(serviceStorage);
                var serviceRegistry = CreateServiceRegistry(serviceStorage, serviceRsaProvider);

                var credentials = await serviceRegistry
                    .GetCredentialsAsync(serviceIdentity, cancellationToken)
                    .ConfigureAwait(false);

                foreach (var credential in credentials)
                {
                    credentialIds.Add(credential.CredentialId);
                }
            }
            catch (Exception exception)
                when (exception is InvalidOperationException
                    or CryptographicException
                    or IOException
                    or UnauthorizedAccessException)
            {
                localStateError = exception;
            }
        }

        foreach (var credentialId in credentialIds)
        {
            await _hostTrustStore
                .RevokeAsync(serviceIdentity, credentialId, cancellationToken)
                .ConfigureAwait(false);
        }

        await _hostStorage
            .DeleteAsync(GetProvisioningRecordKeyId(context.ServiceName), cancellationToken)
            .ConfigureAwait(false);

        CryptoKitManagementProvisioningPaths.DeleteStorageDirectorySafely(
            context.ServiceRootDirectory,
            storageDirectory);

        if (localStateError is not null)
        {
            throw new InvalidOperationException(
                "Service credential state был повреждён во время deprovision. " +
                "Известные Host credentials отозваны, а локальный CryptoKit storage удалён.",
                localStateError);
        }
    }

    private FileKeyStorage CreateServiceStorage(string storageDirectory)
    {
        return new FileKeyStorage(
            new FileKeyStorageOptions
            {
                DirectoryPath = storageDirectory
            });
    }

    private RsaKeyProvider CreateServiceRsaProvider(IKeyStorage serviceStorage)
    {
        return new RsaKeyProvider(
            serviceStorage,
            new RsaKeyGenerator(),
            new RsaKeyOptions
            {
                KeySize = _options.ServiceRsaKeySize
            });
    }

    private static CryptoKitManagementCredentialRegistry CreateServiceRegistry(
        IKeyStorage serviceStorage,
        IRsaKeyProvider serviceRsaProvider)
    {
        return new CryptoKitManagementCredentialRegistry(
            serviceStorage,
            serviceRsaProvider,
            new CryptoKitManagementOptionsSnapshot(
                new CryptoKitManagementSecurityOptions()));
    }

    private static async ValueTask<CryptoKitManagementPublicCredential> EnsureActiveServiceCredentialAsync(
        ManagementPeerIdentity serviceIdentity,
        CryptoKitManagementCredentialRegistry registry,
        IRsaKeyProvider rsaKeyProvider,
        CancellationToken cancellationToken)
    {
        var credentials = await registry
            .GetCredentialsAsync(serviceIdentity, cancellationToken)
            .ConfigureAwait(false);

        var active = credentials
            .Where(static credential =>
                credential.Status == CryptoKitManagementLocalCredentialStatus.Active)
            .ToArray();

        if (active.Length > 1)
        {
            throw new InvalidOperationException(
                $"Для service identity '{serviceIdentity.PeerId}' найдено несколько active credentials.");
        }

        if (active.Length == 1)
        {
            var publicKey = await rsaKeyProvider
                .GetPublicKeyAsync(active[0].KeyId, cancellationToken)
                .ConfigureAwait(false);

            return new CryptoKitManagementPublicCredential(
                serviceIdentity,
                active[0].CredentialId,
                publicKey,
                active[0].Status);
        }

        var suffix = Guid.NewGuid().ToString("N");
        var keyId = $"apikit.management.service.{suffix}";
        var credentialId = $"{serviceIdentity.PeerId}-{suffix}";

        var publicKey = await rsaKeyProvider
            .GetOrCreatePublicKeyAsync(keyId, cancellationToken)
            .ConfigureAwait(false);

        var credential = await registry
            .PrepareAsync(
                serviceIdentity,
                keyId,
                credentialId,
                createIfMissing: false,
                cancellationToken)
            .ConfigureAwait(false);

        if (!CryptographicOperations.FixedTimeEquals(
                publicKey,
                credential.PublicKeySubjectPublicKeyInfo))
        {
            throw new CryptographicException(
                "Public key provisioned service credential изменился между созданием и регистрацией.");
        }

        return credential;
    }

    private async ValueTask SaveProvisioningRecordAsync(
        string serviceName,
        string credentialId,
        CancellationToken cancellationToken)
    {
        var keyId = GetProvisioningRecordKeyId(serviceName);
        var existing = await TryLoadProvisioningRecordAsync(serviceName, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var record = new StoredProvisioningRecord
            {
                Version = ProvisioningRecordVersion,
                ServiceName = serviceName,
                CredentialIds = [credentialId],
                ProvisionedAtUtc = _timeProvider.GetUtcNow()
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(record);
            if (await _hostStorage.CreateAsync(keyId, bytes, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            existing = await TryLoadProvisioningRecordAsync(serviceName, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    "Provisioning record исчез после конкурентного создания.");
        }

        if (!existing.CredentialIds.Contains(credentialId, StringComparer.Ordinal))
        {
            existing.CredentialIds.Add(credentialId);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(existing);
            await _hostStorage.ReplaceAsync(keyId, bytes, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<StoredProvisioningRecord?> TryLoadProvisioningRecordAsync(
        string serviceName,
        CancellationToken cancellationToken)
    {
        var bytes = await _hostStorage
            .TryLoadAsync(GetProvisioningRecordKeyId(serviceName), cancellationToken)
            .ConfigureAwait(false);

        if (bytes is null)
        {
            return null;
        }

        StoredProvisioningRecord record;
        try
        {
            record = JsonSerializer.Deserialize<StoredProvisioningRecord>(bytes)
                ?? throw new InvalidOperationException("Provisioning record пуст.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Provisioning record имеет некорректный формат.",
                exception);
        }

        if (record.Version != ProvisioningRecordVersion
            || !StringComparer.OrdinalIgnoreCase.Equals(record.ServiceName, serviceName)
            || record.CredentialIds is null)
        {
            throw new CryptographicException(
                "Provisioning record не соответствует запрошенному сервису.");
        }

        return record;
    }

    private static string GetProvisioningRecordKeyId(string serviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var bytes = StrictUtf8.GetBytes(serviceName.ToUpperInvariant());
        try
        {
            return ProvisioningRecordPrefix + Convert.ToHexString(SHA256.HashData(bytes));
        }
        finally
        {
            Array.Clear(bytes, 0, bytes.Length);
        }
    }

    private static void ValidateContext(ManagedServiceCredentialProvisioningContext context)
    {
        ArgumentNullException.ThrowIfNull(context.Manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.Manifest.ServiceName);
        ArgumentNullException.ThrowIfNull(context.ManagementHostIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.ServiceRootDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.InstallDirectory);

        if (context.ManagementHostIdentity.Kind != ManagementPeerKind.ManagementHost)
        {
            throw new ArgumentException(
                "Provisioning требует ManagementHost identity.",
                nameof(context));
        }
    }

    private sealed class StoredProvisioningRecord
    {
        public int Version { get; set; }

        public string ServiceName { get; set; } = string.Empty;

        public List<string> CredentialIds { get; set; } = [];

        public DateTimeOffset ProvisionedAtUtc { get; set; }
    }
}
