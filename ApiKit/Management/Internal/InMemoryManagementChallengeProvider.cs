using System.Security.Cryptography;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Options;
using ApiKit.Management.Security;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Internal;

internal sealed class InMemoryManagementChallengeProvider(
    IOptions<ManagementSecurityOptions> options,
    TimeProvider timeProvider) : IManagementChallengeProvider
{
    private const int ChallengeIdSizeBytes = 16;
    private const int MaximumChallengeIdAttempts = 8;

    private readonly ManagementSecurityOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, ManagementChallenge> _pending =
        new(StringComparer.Ordinal);

    public ValueTask<ManagementChallenge> CreateChallengeAsync(
        ManagementPeerIdentity issuer,
        ManagementPeerIdentity subject,
        string transport,
        string purpose,
        int protocolVersion,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ValidateIdentity(issuer, nameof(issuer));
        ValidateIdentity(subject, nameof(subject));
        ArgumentException.ThrowIfNullOrWhiteSpace(transport);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        if (protocolVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(protocolVersion));
        }

        var now = _timeProvider.GetUtcNow();
        var nonce = RandomNumberGenerator.GetBytes(_options.NonceSizeBytes);

        lock (_syncRoot)
        {
            RemoveExpiredChallenges(now);

            if (_pending.Count >= _options.MaximumPendingChallenges)
            {
                throw new InvalidOperationException(
                    "Достигнут лимит одновременно ожидающих management challenge.");
            }

            for (var attempt = 0; attempt < MaximumChallengeIdAttempts; attempt++)
            {
                var challengeId = Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(ChallengeIdSizeBytes));

                var storedChallenge = new ManagementChallenge
                {
                    ChallengeId = challengeId,
                    Issuer = issuer,
                    Subject = subject,
                    Nonce = nonce.ToArray(),
                    Transport = transport,
                    Purpose = purpose,
                    ProtocolVersion = protocolVersion,
                    IssuedAtUtc = now,
                    ExpiresAtUtc = now + _options.ChallengeLifetime
                };

                if (!_pending.TryAdd(challengeId, storedChallenge))
                {
                    continue;
                }

                return ValueTask.FromResult(Clone(storedChallenge));
            }
        }

        throw new CryptographicException(
            "Не удалось сформировать уникальный идентификатор management challenge.");
    }

    public ValueTask<ManagementChallenge?> ConsumeChallengeAsync(
        string challengeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(challengeId);

        lock (_syncRoot)
        {
            if (!_pending.Remove(challengeId, out var challenge))
            {
                return ValueTask.FromResult<ManagementChallenge?>(null);
            }

            if (_timeProvider.GetUtcNow() > challenge.ExpiresAtUtc)
            {
                return ValueTask.FromResult<ManagementChallenge?>(null);
            }

            return ValueTask.FromResult<ManagementChallenge?>(Clone(challenge));
        }
    }

    private void RemoveExpiredChallenges(DateTimeOffset now)
    {
        if (_pending.Count == 0)
        {
            return;
        }

        var expiredIds = _pending
            .Where(pair => pair.Value.ExpiresAtUtc < now)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var challengeId in expiredIds)
        {
            _pending.Remove(challengeId);
        }
    }

    private static ManagementChallenge Clone(ManagementChallenge challenge) =>
        challenge with
        {
            Nonce = challenge.Nonce.ToArray()
        };

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
}
