using ApiKit.Management.CryptoKit.Options;
using ApiKit.Management.Security;

namespace ApiKit.Management.CryptoKit.Internal;

internal sealed class CryptoKitManagementOptionsSnapshot
{
    private readonly IReadOnlyList<CryptoKitManagementCredentialRegistration> _credentials;

    internal CryptoKitManagementOptionsSnapshot(CryptoKitManagementSecurityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.ClockSkew < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Допустимое расхождение часов не может быть отрицательным.");
        }

        ClockSkew = options.ClockSkew;
        _credentials = ValidateAndCopy(options.Credentials);
    }

    internal TimeSpan ClockSkew { get; }

    internal IReadOnlyList<CryptoKitManagementCredentialRegistration> Credentials => _credentials;

    internal IReadOnlyList<CryptoKitManagementCredentialRegistration> GetConfiguredCredentials(
        ManagementPeerIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return _credentials
            .Where(x =>
                x.Kind == identity.Kind &&
                StringComparer.Ordinal.Equals(x.PeerId, identity.PeerId))
            .ToArray();
    }

    private static IReadOnlyList<CryptoKitManagementCredentialRegistration> ValidateAndCopy(
        IReadOnlyList<CryptoKitManagementCredentialRegistration> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        var copy = new List<CryptoKitManagementCredentialRegistration>(credentials.Count);
        var uniqueCredentials = new HashSet<(ManagementPeerKind Kind, string PeerId, string CredentialId)>();
        var keyOwners = new Dictionary<string, (ManagementPeerKind Kind, string PeerId, string CredentialId)>(StringComparer.Ordinal);

        foreach (var credential in credentials)
        {
            ArgumentNullException.ThrowIfNull(credential);
            ArgumentException.ThrowIfNullOrWhiteSpace(credential.PeerId);
            ArgumentException.ThrowIfNullOrWhiteSpace(credential.KeyId);
            ArgumentException.ThrowIfNullOrWhiteSpace(credential.CredentialId);

            if (credential.Kind == ManagementPeerKind.Unknown ||
                !Enum.IsDefined(credential.Kind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(credentials),
                    $"Неизвестный тип management peer '{credential.Kind}'.");
            }

            var uniqueKey = (credential.Kind, credential.PeerId, credential.CredentialId);

            if (!uniqueCredentials.Add(uniqueKey))
            {
                throw new InvalidOperationException(
                    $"CryptoKit credential '{credential.CredentialId}' повторно зарегистрирован для '{credential.Kind}:{credential.PeerId}'.");
            }

            if (keyOwners.TryGetValue(credential.KeyId, out var existingOwner))
            {
                throw new InvalidOperationException(
                    $"CryptoKit KeyId '{credential.KeyId}' уже используется management credential " +
                    $"'{existingOwner.Kind}:{existingOwner.PeerId}:{existingOwner.CredentialId}'. " +
                    "Каждый management credential должен иметь собственный RSA-ключ.");
            }

            keyOwners.Add(
                credential.KeyId,
                (credential.Kind, credential.PeerId, credential.CredentialId));

            copy.Add(new CryptoKitManagementCredentialRegistration
            {
                PeerId = credential.PeerId,
                Kind = credential.Kind,
                KeyId = credential.KeyId,
                CredentialId = credential.CredentialId,
                IsActive = credential.IsActive,
                CreateIfMissing = credential.CreateIfMissing
            });
        }

        foreach (var group in copy.GroupBy(x => (x.Kind, x.PeerId)))
        {
            if (group.Count(x => x.IsActive) > 1)
            {
                throw new InvalidOperationException(
                    $"Для management identity '{group.Key.Kind}:{group.Key.PeerId}' может существовать только один активный CryptoKit credential.");
            }
        }

        return copy;
    }
}
