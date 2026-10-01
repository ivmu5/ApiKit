using ApiKit.Management.Abstractions;
using ApiKit.Management.CryptoKit.DependencyInjection;
using ApiKit.Management.CryptoKit.Models;
using ApiKit.Management.CryptoKit.Provisioning;
using ApiKit.Management.CryptoKit.Security;
using ApiKit.Management.CryptoKit.Tests.Support;
using ApiKit.Management.Lifecycle;
using ApiKit.Management.Provisioning;
using ApiKit.Management.Security;
using CryptoKit.Rsa;
using CryptoKit.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ApiKit.Management.CryptoKit.Tests.Security;

public sealed class ProvisioningTests
{
    [Fact]
    public async Task Provision_is_idempotent_creates_separate_service_key_and_revokes_on_deprovision()
    {
        var root = Path.Combine(Path.GetTempPath(), "apikit-provision-test-" + Guid.NewGuid().ToString("N"));
        var install = Path.Combine(root, "1.0.0");
        Directory.CreateDirectory(install);
        try
        {
            var hostStorage = new InMemoryKeyStorage();
            var services = new ServiceCollection();
            services.AddSingleton<IKeyStorage>(hostStorage);
            services.AddCryptoKitRsa(options => options.KeySize = 2048);
            services.AddApiKitManagementCryptoKit(options =>
                options.AddCredential("host", ManagementPeerKind.ManagementHost, "host-key", "host-cred", createIfMissing: true));
            services.AddApiKitManagementCryptoKitProvisioning(options => options.ServiceRsaKeySize = 2048);
            using var provider = services.BuildServiceProvider();
            var provisioner = provider.GetRequiredService<IManagedServiceCredentialProvisioner>();
            var hostIdentity = ManagementPeerIdentity.ForManagementHost("host");
            var serviceIdentity = new ManagementPeerIdentity("service-a", ManagementPeerKind.ManagedService);
            var context = new ManagedServiceCredentialProvisioningContext(
                new ManagedServicePackageManifest { ServiceName = "service-a", DisplayName = "Service A",
                    Version = "1.0.0", EntryPoint = "service-a.exe" }, hostIdentity, root, install);

            await provisioner.ProvisionAsync(context);
            var hostTrust = provider.GetRequiredService<ICryptoKitManagementTrustStore>();
            var serviceDir = CryptoKitManagementProvisioningPaths.GetStorageDirectory(root);
            Assert.True(Directory.Exists(serviceDir));
            Assert.NotEmpty(Directory.EnumerateFiles(serviceDir));
            var publicHost = await provider.GetRequiredService<ICryptoKitManagementPublicCredentialProvider>()
                .GetActiveAsync(hostIdentity);

            var serviceServices = new ServiceCollection();
            serviceServices.AddCryptoKitFileStorage(options => options.DirectoryPath = serviceDir);
            serviceServices.AddCryptoKitRsa(options => options.KeySize = 2048);
            serviceServices.AddApiKitManagementCryptoKit(_ => { });
            using var serviceProvider = serviceServices.BuildServiceProvider();
            var servicePublic = serviceProvider.GetRequiredService<ICryptoKitManagementPublicCredentialProvider>();
            var serviceRotation = serviceProvider.GetRequiredService<ICryptoKitManagementCredentialRotationManager>();
            var installed = await serviceRotation.GetCredentialsAsync(serviceIdentity);
            var active = Assert.Single(installed);
            Assert.Equal(CryptoKitManagementLocalCredentialStatus.Active, active.Status);
            var credential = await servicePublic.GetActiveAsync(serviceIdentity);
            Assert.NotNull(await hostTrust.TryGetAsync(serviceIdentity, credential.CredentialId));
            var serviceTrust = serviceProvider.GetRequiredService<ICryptoKitManagementTrustStore>();
            var trustedHost = await serviceTrust.TryGetAsync(hostIdentity, publicHost.CredentialId);
            Assert.NotNull(trustedHost);
            Assert.Equal(publicHost.PublicKeySubjectPublicKeyInfo, trustedHost.PublicKeySubjectPublicKeyInfo);
            Assert.NotEqual(Convert.ToHexString(publicHost.PublicKeySubjectPublicKeyInfo),
                Convert.ToHexString(credential.PublicKeySubjectPublicKeyInfo));
            Assert.DoesNotContain(hostStorage.Snapshot.Keys, key => key.Contains(active.KeyId, StringComparison.Ordinal));

            await provisioner.ProvisionAsync(context);
            Assert.Single(await serviceRotation.GetCredentialsAsync(serviceIdentity));
            await provisioner.DeprovisionAsync(new ManagedServiceCredentialDeprovisioningContext(
                "service-a", hostIdentity, root));
            Assert.False(Directory.Exists(serviceDir));
            Assert.Null(await hostTrust.TryGetAsync(serviceIdentity, credential.CredentialId));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData(".")]
    [InlineData("CON")]
    [InlineData("name/child")]
    public void Unsafe_provisioning_subdirectories_are_rejected(string invalid)
    {
        Assert.Throws<ArgumentException>(() =>
            CryptoKitManagementProvisioningPaths.GetStorageDirectory(Path.GetTempPath(), invalid));
    }
}
