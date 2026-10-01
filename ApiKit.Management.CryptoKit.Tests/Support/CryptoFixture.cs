using ApiKit.Management.Abstractions;
using ApiKit.Management.CryptoKit.DependencyInjection;
using ApiKit.Management.CryptoKit.Options;
using ApiKit.Management.CryptoKit.Security;
using ApiKit.Management.Models;
using ApiKit.Management.Security;
using CryptoKit.Rsa;
using CryptoKit.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ApiKit.Management.CryptoKit.Tests.Support;

internal sealed class CryptoFixture : IDisposable
{
    internal static readonly ManagementPeerIdentity Admin = ManagementPeerIdentity.ForAdminClient("test-admin");
    internal static readonly ManagementPeerIdentity Host = ManagementPeerIdentity.ForManagementHost("test-host");
    internal static readonly DateTimeOffset Epoch = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private readonly ServiceProvider _services;

    internal CryptoFixture(InMemoryKeyStorage? sharedStorage = null, Action<CryptoKitManagementSecurityOptions>? configure = null)
    {
        Storage = sharedStorage ?? new InMemoryKeyStorage();
        Clock = new ManualTimeProvider(Epoch);
        var services = new ServiceCollection();
        services.AddSingleton<IKeyStorage>(Storage);
        services.AddSingleton<TimeProvider>(Clock);
        services.AddCryptoKitRsa(options => options.KeySize = 2048);
        services.AddApiKitManagementCryptoKit(configure ?? (options =>
            options.AddCredential(Admin.PeerId, Admin.Kind, "admin-key-1", "admin-credential-1", createIfMissing: true)));
        _services = services.BuildServiceProvider();
    }

    internal InMemoryKeyStorage Storage { get; }
    internal ManualTimeProvider Clock { get; }
    internal IManagementCredentialProvider Credentials => Get<IManagementCredentialProvider>();
    internal IManagementPeerVerifier Verifier => Get<IManagementPeerVerifier>();
    internal ICryptoKitManagementTrustStore Trust => Get<ICryptoKitManagementTrustStore>();
    internal ICryptoKitManagementPublicCredentialProvider Public => Get<ICryptoKitManagementPublicCredentialProvider>();
    internal ICryptoKitManagementCredentialRotationManager Rotation => Get<ICryptoKitManagementCredentialRotationManager>();
    internal T Get<T>() where T : notnull => _services.GetRequiredService<T>();

    internal ManagementChallenge Challenge(
        ManagementPeerIdentity? subject = null,
        ManagementPeerIdentity? issuer = null) => new()
        {
            ChallengeId = Guid.NewGuid().ToString("N"),
            Issuer = issuer ?? Host,
            Subject = subject ?? Admin,
            Nonce = Enumerable.Range(0, 32).Select(static i => (byte)i).ToArray(),
            Transport = "named-pipe",
            Purpose = "administration",
            ProtocolVersion = 6,
            IssuedAtUtc = Clock.GetUtcNow(),
            ExpiresAtUtc = Clock.GetUtcNow().AddMinutes(1)
        };

    internal static ManagementPeerContext Peer(string purpose = "administration", string transport = "named-pipe") => new()
    {
        Transport = transport,
        Properties = new Dictionary<string, string> { ["purpose"] = purpose }
    };

    internal async Task TrustAdminAsync()
    {
        var credential = await Public.GetActiveAsync(Admin);
        await Trust.TrustAsync(Admin, credential.CredentialId, credential.PublicKeySubjectPublicKeyInfo);
    }

    public void Dispose() => _services.Dispose();
}

internal sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private long _ticks = start.UtcDateTime.Ticks;
    public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);
    internal void Advance(TimeSpan by) => Interlocked.Add(ref _ticks, by.Ticks);
}
