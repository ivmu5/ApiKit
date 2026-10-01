using ApiKit.Management.Windows.Tests.Support;
using System.Security.Principal;
using ApiKit.Management.Windows.Internal;
using ApiKit.Management.Windows.Options;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Windows.Tests.Installer;

public sealed class IsolationRegistryTests
{
    [WindowsFact]
    public async Task Only_valid_provisioned_service_sids_are_exposed_to_registration_pipe()
    {
        var installRoot = Path.Combine(Path.GetTempPath(), "apikit-isolation-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(installRoot);
        try
        {
            var one = Path.Combine(installRoot, "service-a");
            Directory.CreateDirectory(one);
            var sid = new SecurityIdentifier("S-1-5-80-100-200-300-400-500");
            WindowsManagedServiceIsolationState.Save(one, new WindowsManagedServiceIsolationState
            {
                Version = WindowsManagedServiceIsolationState.CurrentVersion,
                ServiceName = "service-a",
                ServiceSid = sid.Value,
                ManagementHostSid = sid.Value,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            var impostor = Path.Combine(installRoot, "service-c");
            WindowsManagedServiceIsolationState.Save(impostor, new WindowsManagedServiceIsolationState
            {
                Version = WindowsManagedServiceIsolationState.CurrentVersion,
                ServiceName = "service-a", // The directory name differs from the stored service identity.
                ServiceSid = "S-1-5-80-10-20-30-40-50",
                ManagementHostSid = sid.Value,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            var broken = Path.Combine(installRoot, "service-b");
            Directory.CreateDirectory(WindowsManagedServiceIsolationState.GetStateDirectory(broken));
            File.WriteAllText(WindowsManagedServiceIsolationState.GetStatePath(broken), "{corrupt");
            var registry = new WindowsManagedServiceIsolationRegistry(Options.Create(
                new WindowsServiceManagementOptions { InstallRoot = installRoot }));
            Assert.Equal(new[] { sid.Value }, registry.GetProvisionedServiceSids());
            var version = registry.Version;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var waiter = registry.WaitForChangeAsync(version, timeout.Token);
            Assert.False(waiter.IsCompleted);
            registry.NotifyChanged();
            await waiter;
            Assert.Equal(version + 1, registry.Version);
        }
        finally
        {
            WindowsServicePackageStorage.DeleteDirectoryIfExists(installRoot);
        }
    }

    [WindowsFact]
    public void Isolation_state_rejects_corrupted_sids_and_unexpected_format_version()
    {
        var root = Path.Combine(Path.GetTempPath(), "apikit-state-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var sid = "S-1-5-80-100-200-300-400-500";
            var state = new WindowsManagedServiceIsolationState
            {
                Version = WindowsManagedServiceIsolationState.CurrentVersion,
                ServiceName = "test",
                ServiceSid = sid,
                ManagementHostSid = sid,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            Assert.Throws<InvalidOperationException>(() =>
                WindowsManagedServiceIsolationState.Save(root, state with { Version = 99 }));
            Assert.ThrowsAny<ArgumentException>(() =>
                WindowsManagedServiceIsolationState.Save(root, state with { ServiceSid = "not-a-sid" }));
            Assert.Throws<ArgumentException>(() =>
                WindowsManagedServiceIsolationState.Save(root, state with { ServiceSid = "S-1-5-80-0" }));
            Assert.Throws<ArgumentException>(() =>
                WindowsManagedServiceIsolationState.Save(root, state with { ServiceSid = "S-1-5-32-544" }));
            WindowsManagedServiceIsolationState.Save(root, state);
            Assert.Equal(sid, WindowsManagedServiceIsolationState.TryLoad(root)?.ServiceSid);
            WindowsManagedServiceIsolationState.Delete(root);
            Assert.Null(WindowsManagedServiceIsolationState.TryLoad(root));
        }
        finally
        {
            WindowsServicePackageStorage.DeleteDirectoryIfExists(root);
        }
    }
}
