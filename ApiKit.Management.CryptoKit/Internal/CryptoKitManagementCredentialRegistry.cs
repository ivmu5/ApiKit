using System.Text.Json;
using ApiKit.Management.CryptoKit.Models;
using ApiKit.Management.CryptoKit.Options;
using ApiKit.Management.CryptoKit.Security;
using ApiKit.Management.Security;
using CryptoKit.Rsa;
using CryptoKit.Storage;

namespace ApiKit.Management.CryptoKit.Internal;

/// <summary>
/// Persists and rotates local management signing credentials.
/// </summary>
internal sealed class CryptoKitManagementCredentialRegistry(
    IKeyStorage storage,
    IRsaKeyProvider rsaKeyProvider,
    CryptoKitManagementOptionsSnapshot options)
    : ICryptoKitManagementCredentialRotationManager
{
    private const string StateStorageKeyId = "apikit.management.local-credentials.v1";
    private const int StateFormatVersion = 1;

    private readonly IKeyStorage _storage = storage
        ?? throw new ArgumentNullException(nameof(storage));

    private readonly IRsaKeyProvider _rsaKeyProvider = rsaKeyProvider
        ?? throw new ArgumentNullException(nameof(rsaKeyProvider));

    private readonly CryptoKitManagementOptionsSnapshot _options = options
        ?? throw new ArgumentNullException(nameof(options));

    private readonly SemaphoreSlim _gate = new(1, 1);

    public async ValueTask<IReadOnlyList<CryptoKitManagementCredentialInfo>> GetCredentialsAsync(
        ManagementPeerIdentity identity,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentity(identity);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadMergedStateAsync(cancellationToken).ConfigureAwait(false);
            return state.Credentials
                .Where(x => MatchesIdentity(x, identity))
                .Select(x => ToInfo(x, identity))
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask<CryptoKitManagementPublicCredential> PrepareAsync(
        ManagementPeerIdentity identity,
        string keyId,
        string credentialId,
        bool createIfMissing = true,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentity(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadMergedStateAsync(cancellationToken).ConfigureAwait(false);
            var existing = state.Credentials.FirstOrDefault(x =>
                MatchesIdentity(x, identity) &&
                StringComparer.Ordinal.Equals(x.CredentialId, credentialId));

            if (existing is not null)
            {
                if (!StringComparer.Ordinal.Equals(existing.KeyId, keyId))
                {
                    throw new InvalidOperationException(
                        $"Credential '{credentialId}' уже связан с другим CryptoKit KeyId.");
                }

                if (existing.Status == CryptoKitManagementLocalCredentialStatus.Retired)
                {
                    throw new InvalidOperationException(
                        $"Credential '{credentialId}' уже retired и не может быть подготовлен повторно.");
                }

                var existingPublicKey = existing.CreateIfMissing
                    ? await _rsaKeyProvider.GetOrCreatePublicKeyAsync(existing.KeyId, cancellationToken).ConfigureAwait(false)
                    : await _rsaKeyProvider.GetPublicKeyAsync(existing.KeyId, cancellationToken).ConfigureAwait(false);

                return new CryptoKitManagementPublicCredential(
                    identity,
                    credentialId,
                    existingPublicKey,
                    existing.Status);
            }

            var keyOwner = state.Credentials.FirstOrDefault(x =>
                StringComparer.Ordinal.Equals(x.KeyId, keyId));

            if (keyOwner is not null)
            {
                throw new InvalidOperationException(
                    $"CryptoKit KeyId '{keyId}' уже принадлежит management credential " +
                    $"'{keyOwner.Kind}:{keyOwner.PeerId}:{keyOwner.CredentialId}'.");
            }

            var publicKey = createIfMissing
                ? await _rsaKeyProvider.GetOrCreatePublicKeyAsync(keyId, cancellationToken).ConfigureAwait(false)
                : await _rsaKeyProvider.GetPublicKeyAsync(keyId, cancellationToken).ConfigureAwait(false);

            var hasActive = state.Credentials.Any(x =>
                MatchesIdentity(x, identity) &&
                x.Status == CryptoKitManagementLocalCredentialStatus.Active);

            var entry = new StoredCredential
            {
                PeerId = identity.PeerId,
                Kind = identity.Kind,
                KeyId = keyId,
                CredentialId = credentialId,
                Status = hasActive
                    ? CryptoKitManagementLocalCredentialStatus.Available
                    : CryptoKitManagementLocalCredentialStatus.Active,
                CreateIfMissing = createIfMissing
            };

            state.Credentials.Add(entry);
            ValidateState(state);
            await SaveExistingStateAsync(state, cancellationToken).ConfigureAwait(false);

            return new CryptoKitManagementPublicCredential(
                identity,
                credentialId,
                publicKey,
                entry.Status);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask ActivateAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentity(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadMergedStateAsync(cancellationToken).ConfigureAwait(false);
            var target = FindCredential(state, identity, credentialId);

            if (target.Status == CryptoKitManagementLocalCredentialStatus.Retired)
            {
                throw new InvalidOperationException(
                    $"Credential '{credentialId}' retired и не может быть повторно активирован.");
            }

            if (target.CreateIfMissing)
            {
                await _rsaKeyProvider.GetOrCreatePublicKeyAsync(target.KeyId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _rsaKeyProvider.GetPublicKeyAsync(target.KeyId, cancellationToken).ConfigureAwait(false);
            }

            foreach (var credential in state.Credentials.Where(x => MatchesIdentity(x, identity)))
            {
                if (ReferenceEquals(credential, target))
                {
                    credential.Status = CryptoKitManagementLocalCredentialStatus.Active;
                }
                else if (credential.Status == CryptoKitManagementLocalCredentialStatus.Active)
                {
                    credential.Status = CryptoKitManagementLocalCredentialStatus.Available;
                }
            }

            ValidateState(state);
            await SaveExistingStateAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask RetireAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentity(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadMergedStateAsync(cancellationToken).ConfigureAwait(false);
            var target = FindCredential(state, identity, credentialId);

            if (target.Status == CryptoKitManagementLocalCredentialStatus.Active)
            {
                throw new InvalidOperationException(
                    "Нельзя retire активный management credential. Сначала активируйте заменяющий credential.");
            }

            if (target.Status == CryptoKitManagementLocalCredentialStatus.Retired)
            {
                return;
            }

            target.Status = CryptoKitManagementLocalCredentialStatus.Retired;
            ValidateState(state);
            await SaveExistingStateAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async ValueTask<CryptoKitManagementCredentialRegistration> GetActiveCredentialAsync(
        ManagementPeerIdentity identity,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(identity);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadMergedStateAsync(cancellationToken).ConfigureAwait(false);
            var active = state.Credentials
                .Where(x => MatchesIdentity(x, identity) &&
                            x.Status == CryptoKitManagementLocalCredentialStatus.Active)
                .ToArray();

            return active.Length switch
            {
                1 => ToRegistration(active[0]),
                0 => throw new InvalidOperationException(
                    $"Для management identity '{identity.Kind}:{identity.PeerId}' не зарегистрирован активный CryptoKit credential."),
                _ => throw new InvalidOperationException(
                    $"Для management identity '{identity.Kind}:{identity.PeerId}' зарегистрировано несколько активных CryptoKit credentials.")
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async ValueTask<(CryptoKitManagementCredentialRegistration Registration, CryptoKitManagementLocalCredentialStatus Status)> GetCredentialAsync(
        ManagementPeerIdentity identity,
        string credentialId,
        CancellationToken cancellationToken)
    {
        ValidateIdentity(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialId);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadMergedStateAsync(cancellationToken).ConfigureAwait(false);
            var credential = FindCredential(state, identity, credentialId);
            return (ToRegistration(credential), credential.Status);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async ValueTask<StoredState> LoadMergedStateAsync(CancellationToken cancellationToken)
    {
        var bytes = await _storage.TryLoadAsync(StateStorageKeyId, cancellationToken).ConfigureAwait(false);
        StoredState state;
        var exists = bytes is not null;

        if (bytes is null)
        {
            state = CreateInitialState();
        }
        else
        {
            try
            {
                state = JsonSerializer.Deserialize<StoredState>(bytes)
                    ?? throw new InvalidOperationException("Состояние management credential rotation пусто.");
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    "Состояние management credential rotation имеет некорректный формат.",
                    exception);
            }

            ValidateState(state);
        }

        var changed = MergeConfiguredCredentials(state);
        ValidateState(state);

        if (!exists)
        {
            var serialized = JsonSerializer.SerializeToUtf8Bytes(state);
            if (!await _storage.CreateAsync(StateStorageKeyId, serialized, cancellationToken).ConfigureAwait(false))
            {
                var published = await _storage.TryLoadAsync(StateStorageKeyId, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Management credential state исчез после конкурентной инициализации.");
                state = JsonSerializer.Deserialize<StoredState>(published)
                    ?? throw new InvalidOperationException("Опубликованное management credential state пусто.");
                ValidateState(state);
                if (MergeConfiguredCredentials(state))
                {
                    await SaveExistingStateAsync(state, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        else if (changed)
        {
            await SaveExistingStateAsync(state, cancellationToken).ConfigureAwait(false);
        }

        return state;
    }

    private StoredState CreateInitialState()
    {
        var state = new StoredState { Version = StateFormatVersion };

        foreach (var credential in _options.Credentials)
        {
            state.Credentials.Add(new StoredCredential
            {
                PeerId = credential.PeerId,
                Kind = credential.Kind,
                KeyId = credential.KeyId,
                CredentialId = credential.CredentialId,
                Status = credential.IsActive
                    ? CryptoKitManagementLocalCredentialStatus.Active
                    : CryptoKitManagementLocalCredentialStatus.Available,
                CreateIfMissing = credential.CreateIfMissing
            });
        }

        return state;
    }

    private bool MergeConfiguredCredentials(StoredState state)
    {
        var changed = false;

        foreach (var configured in _options.Credentials)
        {
            var existing = state.Credentials.FirstOrDefault(x =>
                x.Kind == configured.Kind &&
                StringComparer.Ordinal.Equals(x.PeerId, configured.PeerId) &&
                StringComparer.Ordinal.Equals(x.CredentialId, configured.CredentialId));

            if (existing is not null)
            {
                if (!StringComparer.Ordinal.Equals(existing.KeyId, configured.KeyId))
                {
                    throw new InvalidOperationException(
                        $"Configured credential '{configured.Kind}:{configured.PeerId}:{configured.CredentialId}' " +
                        "не совпадает с durable rotation state по KeyId.");
                }

                continue;
            }

            var keyOwner = state.Credentials.FirstOrDefault(x =>
                StringComparer.Ordinal.Equals(x.KeyId, configured.KeyId));
            if (keyOwner is not null)
            {
                throw new InvalidOperationException(
                    $"Configured CryptoKit KeyId '{configured.KeyId}' уже принадлежит другому durable management credential.");
            }

            var hasActive = state.Credentials.Any(x =>
                x.Kind == configured.Kind &&
                StringComparer.Ordinal.Equals(x.PeerId, configured.PeerId) &&
                x.Status == CryptoKitManagementLocalCredentialStatus.Active);

            state.Credentials.Add(new StoredCredential
            {
                PeerId = configured.PeerId,
                Kind = configured.Kind,
                KeyId = configured.KeyId,
                CredentialId = configured.CredentialId,
                Status = configured.IsActive && !hasActive
                    ? CryptoKitManagementLocalCredentialStatus.Active
                    : CryptoKitManagementLocalCredentialStatus.Available,
                CreateIfMissing = configured.CreateIfMissing
            });
            changed = true;
        }

        return changed;
    }

    private async ValueTask SaveExistingStateAsync(
        StoredState state,
        CancellationToken cancellationToken)
    {
        var serialized = JsonSerializer.SerializeToUtf8Bytes(state);
        await _storage.ReplaceAsync(StateStorageKeyId, serialized, cancellationToken).ConfigureAwait(false);
    }

    private static StoredCredential FindCredential(
        StoredState state,
        ManagementPeerIdentity identity,
        string credentialId)
    {
        return state.Credentials.FirstOrDefault(x =>
                   MatchesIdentity(x, identity) &&
                   StringComparer.Ordinal.Equals(x.CredentialId, credentialId))
               ?? throw new KeyNotFoundException(
                   $"Management credential '{credentialId}' для '{identity.Kind}:{identity.PeerId}' не найден.");
    }

    private static bool MatchesIdentity(StoredCredential credential, ManagementPeerIdentity identity)
    {
        return credential.Kind == identity.Kind &&
               StringComparer.Ordinal.Equals(credential.PeerId, identity.PeerId);
    }

    private static CryptoKitManagementCredentialRegistration ToRegistration(StoredCredential credential)
    {
        return new CryptoKitManagementCredentialRegistration
        {
            PeerId = credential.PeerId,
            Kind = credential.Kind,
            KeyId = credential.KeyId,
            CredentialId = credential.CredentialId,
            IsActive = credential.Status == CryptoKitManagementLocalCredentialStatus.Active,
            CreateIfMissing = credential.CreateIfMissing
        };
    }

    private static CryptoKitManagementCredentialInfo ToInfo(
        StoredCredential credential,
        ManagementPeerIdentity identity)
    {
        return new CryptoKitManagementCredentialInfo(
            identity,
            credential.KeyId,
            credential.CredentialId,
            credential.Status,
            credential.CreateIfMissing);
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

    private static void ValidateState(StoredState state)
    {
        if (state.Version != StateFormatVersion)
        {
            throw new InvalidOperationException(
                $"Неподдерживаемая версия management credential state: {state.Version}.");
        }

        state.Credentials ??= [];
        var credentialIds = new HashSet<(ManagementPeerKind Kind, string PeerId, string CredentialId)>();
        var keyIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var credential in state.Credentials)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(credential.PeerId);
            ArgumentException.ThrowIfNullOrWhiteSpace(credential.KeyId);
            ArgumentException.ThrowIfNullOrWhiteSpace(credential.CredentialId);

            if (credential.Kind == ManagementPeerKind.Unknown || !Enum.IsDefined(credential.Kind))
            {
                throw new InvalidOperationException("Durable management credential state содержит неизвестный тип peer.");
            }

            if (!Enum.IsDefined(credential.Status))
            {
                throw new InvalidOperationException("Durable management credential state содержит неизвестный status.");
            }

            if (!credentialIds.Add((credential.Kind, credential.PeerId, credential.CredentialId)))
            {
                throw new InvalidOperationException("Durable management credential state содержит дублирующий CredentialId.");
            }

            if (!keyIds.Add(credential.KeyId))
            {
                throw new InvalidOperationException("Durable management credential state содержит повторно используемый CryptoKit KeyId.");
            }
        }

        foreach (var group in state.Credentials.GroupBy(x => (x.Kind, x.PeerId)))
        {
            if (group.Count(x => x.Status == CryptoKitManagementLocalCredentialStatus.Active) > 1)
            {
                throw new InvalidOperationException(
                    $"Durable state содержит несколько active credentials для '{group.Key.Kind}:{group.Key.PeerId}'.");
            }
        }
    }

    public sealed class StoredState
    {
        public int Version { get; set; }

        public List<StoredCredential> Credentials { get; set; } = [];
    }

    public sealed class StoredCredential
    {
        public string PeerId { get; set; } = string.Empty;

        public ManagementPeerKind Kind { get; set; }

        public string KeyId { get; set; } = string.Empty;

        public string CredentialId { get; set; } = string.Empty;

        public CryptoKitManagementLocalCredentialStatus Status { get; set; }

        public bool CreateIfMissing { get; set; }
    }
}
