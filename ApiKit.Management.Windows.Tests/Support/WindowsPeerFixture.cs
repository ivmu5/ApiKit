using ApiKit.Management;
using ApiKit.Management.Abstractions;
using ApiKit.Management.CryptoKit.DependencyInjection;
using ApiKit.Management.CryptoKit.Security;
using ApiKit.Management.Security;
using CryptoKit.Rsa;
using CryptoKit.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ApiKit.Management.Windows.Tests.Support;

/// <summary>
/// Creates isolated test peers with independently generated credentials.
/// </summary>
internal sealed class WindowsPeerFixture : IDisposable
{
    private readonly ServiceProvider _provider;
    internal WindowsPeerFixture(ManagementPeerIdentity identity)
    {
        Identity = identity;
        var services = new ServiceCollection();
        services.AddSingleton<IKeyStorage>(new InMemoryKeyStorage());
        services.AddApiKitManagementSecurity();
        services.AddCryptoKitRsa(options => options.KeySize = 2048);
        services.AddApiKitManagementCryptoKit(options => options.AddCredential(
            identity.PeerId, identity.Kind, "rsa-" + identity.PeerId, "credential-" + identity.PeerId,
            createIfMissing: true));
        _provider = services.BuildServiceProvider();
    }

    internal ManagementPeerIdentity Identity { get; }
    internal IManagementCredentialProvider Credentials => Get<IManagementCredentialProvider>();
    internal IManagementPeerVerifier Verifier => Get<IManagementPeerVerifier>();
    internal IManagementChallengeProvider Challenges => Get<IManagementChallengeProvider>();
    internal ICryptoKitManagementTrustStore Trust => Get<ICryptoKitManagementTrustStore>();
    internal T Get<T>() where T : notnull => _provider.GetRequiredService<T>();

    internal async Task TrustPeerAsync(WindowsPeerFixture other)
    {
        var publicKey = await other.Get<ICryptoKitManagementPublicCredentialProvider>().GetActiveAsync(other.Identity);
        await Trust.TrustAsync(other.Identity, publicKey.CredentialId, publicKey.PublicKeySubjectPublicKeyInfo);
    }

    public void Dispose() => _provider.Dispose();
}
