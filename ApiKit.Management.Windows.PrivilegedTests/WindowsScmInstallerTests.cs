using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Lifecycle;
using ApiKit.Management.Windows;
using ApiKit.Management.Windows.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace ApiKit.Management.Windows.PrivilegedTests;

/// <summary>
/// Explicit-only real Windows SCM and DACL smoke tests. Never use a production install root.
/// Uses a random test service name and a dedicated temp directory.
/// </summary>
public sealed class WindowsScmInstallerTests
{
    [WindowsAdminFact]
    public async Task Installer_refuses_to_modify_an_unmanaged_windows_service()
    {
        var installRoot = Path.Combine(Path.GetTempPath(), "apikit-ownership-" + Guid.NewGuid().ToString("N"));
        var source = Path.Combine(installRoot, "source");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "service.exe"), "never executed");
        using var provider = CreateInstaller(Path.Combine(installRoot, "managed"));
        var installer = provider.GetRequiredService<IManagedServiceInstaller>();
        var lifecycle = provider.GetRequiredService<IManagedServiceLifecycleController>();
        try
        {
            if (await lifecycle.GetStateAsync("EventLog") != ManagedServiceState.Unknown)
            {
                var manifest = Manifest("EventLog", "1.0.0") with { EntryPoint = "service.exe" };
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await installer.UpdateAsync(manifest, source));
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await installer.UninstallAsync("EventLog"));
            }
        }
        finally
        {
            WindowsServicePackageStorage.DeleteDirectoryIfExists(installRoot);
        }
    }

    [WindowsAdminFact]
    public async Task Install_start_stop_update_and_uninstall_use_real_scm_and_service_sid()
    {
        var testName = "ApiKitTest" + Guid.NewGuid().ToString("N")[..10];
        var root = Path.Combine(Path.GetTempPath(), "apikit-scm-test-" + Guid.NewGuid().ToString("N"));
        var sourceV1 = Path.Combine(root, "source-v1");
        var sourceV2 = Path.Combine(root, "source-v2");
        var installRoot = Path.Combine(root, "install");
        Directory.CreateDirectory(sourceV1);
        Directory.CreateDirectory(sourceV2);
        var source = LocateWorkerOutput();
        CopyRuntimeFiles(source, sourceV1);
        CopyRuntimeFiles(source, sourceV2);
        File.WriteAllText(Path.Combine(sourceV1, "build.txt"), "v1");
        File.WriteAllText(Path.Combine(sourceV2, "build.txt"), "v2");
        var manifestV1 = Manifest(testName, "1.0.0");
        var manifestV2 = Manifest(testName, "2.0.0");

        using var provider = CreateInstaller(installRoot);
        var installer = provider.GetRequiredService<IManagedServiceInstaller>();
        var lifecycle = provider.GetRequiredService<IManagedServiceLifecycleController>();
        var serviceRoot = Path.Combine(installRoot, testName);
        try
        {
            await installer.InstallAsync(manifestV1, sourceV1);
            Assert.True(File.Exists(Path.Combine(serviceRoot, "1.0.0", "build.txt")));
            var isolation = WindowsManagedServiceIsolationState.TryLoad(serviceRoot);
            Assert.NotNull(isolation);
            var serviceSid = new SecurityIdentifier(isolation.ServiceSid);
            Assert.True(serviceSid.Value.StartsWith("S-1-5-80-", StringComparison.OrdinalIgnoreCase));

            AssertContainsSid(Path.Combine(serviceRoot, "1.0.0"), serviceSid);
            AssertContainsSid(Path.Combine(serviceRoot, ".management"), serviceSid);

            await lifecycle.StartAsync(testName);
            Assert.Equal(ManagedServiceState.Running, await lifecycle.GetStateAsync(testName));
            await lifecycle.StopAsync(testName);
            Assert.Equal(ManagedServiceState.Stopped, await lifecycle.GetStateAsync(testName));

            await installer.UpdateAsync(manifestV2, sourceV2);
            Assert.Equal("v1", File.ReadAllText(Path.Combine(serviceRoot, "1.0.0", "build.txt")));
            Assert.Equal("v2", File.ReadAllText(Path.Combine(serviceRoot, "2.0.0", "build.txt")));
            Assert.NotNull(WindowsManagedServiceIsolationState.TryLoad(serviceRoot));
            await lifecycle.StartAsync(testName);
            Assert.Equal(ManagedServiceState.Running, await lifecycle.GetStateAsync(testName));
            await lifecycle.StopAsync(testName);

            var invalid = manifestV2 with { EntryPoint = "absent.exe" };
            await Assert.ThrowsAnyAsync<Exception>(async () => await installer.UpdateAsync(invalid, sourceV1));
            Assert.Equal("v2", File.ReadAllText(Path.Combine(serviceRoot, "2.0.0", "build.txt")));

            await installer.UninstallAsync(testName);
            Assert.False(Directory.Exists(serviceRoot));
        }
        finally
        {
            try { await installer.UninstallAsync(testName); }
            catch { try { await WindowsServiceControlCommands.TryDeleteAsync(testName); } catch { } }
            if (Directory.Exists(root))
            {
                try { WindowsServicePackageStorage.DeleteDirectoryIfExists(root); }
                catch { /* Failed cleanup must not mask the diagnostic failure. */ }
            }
        }
    }

    private static ManagedServicePackageManifest Manifest(string name, string version) => new()
    {
        ServiceName = name,
        DisplayName = "ApiKit privileged test " + name,
        Version = version,
        EntryPoint = "ApiKit.Management.Windows.TestWorker.exe",
        Arguments = ["scm", name]
    };

    private static ServiceProvider CreateInstaller(string installRoot)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiKitWindowsManagementHost(
            options => options.UseProvisionedServiceSidAcl = false,
            options =>
            {
                options.InstallRoot = installRoot;
                options.ServiceAccount = @"NT AUTHORITY\LocalService";
                options.ProtectManagedDirectories = true;
                options.EnableServiceSidIsolation = true;
                // Suite is explicitly privileged but does not test CryptoKit provisioning here.
                options.RequirePackageVerifier = false;
                options.RequireCredentialProvisioner = false;
                options.StartAfterInstall = false;
                options.WaitForAuthenticatedRegistration = false;
            });
        return services.BuildServiceProvider();
    }

    private static void AssertContainsSid(string directory, SecurityIdentifier sid)
    {
        var rules = new DirectoryInfo(directory).GetAccessControl(AccessControlSections.Access)
            .GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier));
        Assert.Contains(rules.Cast<FileSystemAccessRule>(), rule =>
            rule.AccessControlType == AccessControlType.Allow && rule.IdentityReference.Equals(sid));
    }

    private static string LocateWorkerOutput()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        var configuration = directory.Parent!.Name;
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ApiKit.slnx")))
            directory = directory.Parent;
        if (directory is null) throw new DirectoryNotFoundException("ApiKit.slnx not found.");
        var bin = Path.Combine(directory.FullName, "ApiKit.Management.Windows.TestWorker",
            "bin", configuration, "net10.0-windows");
        if (!File.Exists(Path.Combine(bin, "ApiKit.Management.Windows.TestWorker.exe")))
            throw new FileNotFoundException("Build the TestWorker apphost for Windows first.", bin);
        return bin;
    }

    private static void CopyRuntimeFiles(string source, string destination)
    {
        foreach (var file in Directory.EnumerateFiles(source))
        {
            var extension = Path.GetExtension(file);
            if (extension is ".exe" or ".dll" or ".json")
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }
    }
}
