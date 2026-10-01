using System.Diagnostics;
using System.Text.Json;
using ApiKit.Management.CryptoKit.DependencyInjection;
using ApiKit.Management.CryptoKit.Security;
using ApiKit.Management.Security;
using ApiKit.Management.Windows.TestWorker;
using ApiKit.Management.Windows.Tests.Support;
using CryptoKit.Rsa;
using CryptoKit.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ApiKit.Management.Windows.Tests.Security;

/// <summary>
/// Tests a real multi-process management host and its connected services.
/// </summary>
public sealed class MultiProcessManagementTests
{
    [WindowsFact]
    public async Task Host_admin_two_services_and_forged_admin_are_isolated_across_processes()
    {
        using var directories = new TestDirectories();
        var host = new StoredPeer(directories.Path("host"), "host", ManagementPeerKind.ManagementHost);
        var admin = new StoredPeer(directories.Path("admin"), "admin", ManagementPeerKind.AdminClient);
        var serviceA = new StoredPeer(directories.Path("service-a"), "service-a", ManagementPeerKind.ManagedService);
        var serviceB = new StoredPeer(directories.Path("service-b"), "service-b", ManagementPeerKind.ManagedService);
        var rogue = new StoredPeer(directories.Path("rogue-admin"), "admin", ManagementPeerKind.AdminClient);
        var rogueService = new StoredPeer(directories.Path("rogue-service"), "untrusted-service", ManagementPeerKind.ManagedService);
        try
        {
            await Task.WhenAll(host.InitAsync(), admin.InitAsync(), serviceA.InitAsync(), serviceB.InitAsync(),
                rogue.InitAsync(), rogueService.InitAsync());
            await Task.WhenAll(
                host.TrustAsync(admin), host.TrustAsync(serviceA), host.TrustAsync(serviceB),
                admin.TrustAsync(host), serviceA.TrustAsync(host), serviceB.TrustAsync(host),
                rogue.TrustAsync(host), rogueService.TrustAsync(host));

            var pipePrefix = "apikit-process-" + Guid.NewGuid().ToString("N");
            WorkerSettings Settings(StoredPeer peer, string? instanceId = null) => new()
            {
                StorageDirectory = peer.Directory,
                PeerId = peer.Identity.PeerId,
                HostPeerId = host.Identity.PeerId,
                InstanceId = instanceId,
                RegistrationPipeName = pipePrefix + ".registration",
                AdminPipeName = pipePrefix + ".admin",
                ServicePipePrefix = pipePrefix + ".service",
                InstallRoot = directories.Path("install-root")
            };

            using var hostProcess = await WorkerProcess.StartServerAsync("host", Settings(host), directories);
            using var firstService = await WorkerProcess.StartServerAsync(
                "service", Settings(serviceA, "instance-a"), directories);
            using var secondService = await WorkerProcess.StartServerAsync(
                "service", Settings(serviceB, "instance-b"), directories);

            string[] names = [];
            for (var attempt = 0; attempt < 30; attempt++)
            {
                var query = await WorkerProcess.QueryAdminAsync(Settings(admin), directories);
                if (query.ExitCode == 0)
                {
                    names = ReadNames(query.Output);
                    if (names.Contains("service-a") && names.Contains("service-b")) break;
                }
                await Task.Delay(250);
            }
            Assert.Contains("service-a", names);
            Assert.Contains("service-b", names);
            Assert.Equal(2, names.Length);

            var impersonation = await WorkerProcess.QueryAdminAsync(Settings(rogue), directories);
            Assert.NotEqual(0, impersonation.ExitCode);
            Assert.DoesNotContain("RESULT:", impersonation.Output);

            using var untrusted = await WorkerProcess.StartServerAsync(
                "service", Settings(rogueService, "rogue-instance"), directories);
            await Task.Delay(1500);
            var stable = await WorkerProcess.QueryAdminAsync(Settings(admin), directories);
            Assert.Equal(0, stable.ExitCode);
            Assert.Equal(new[] { "service-a", "service-b" }, ReadNames(stable.Output).OrderBy(static x => x).ToArray());

            await firstService.StopAsync();
            string[] afterWithdrawal = names;
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var query = await WorkerProcess.QueryAdminAsync(Settings(admin), directories);
                if (query.ExitCode == 0)
                {
                    afterWithdrawal = ReadNames(query.Output);
                    if (!afterWithdrawal.Contains("service-a")) break;
                }
                await Task.Delay(250);
            }
            Assert.Equal(new[] { "service-b" }, afterWithdrawal);
        }
        finally
        {
            host.Dispose();
            admin.Dispose();
            serviceA.Dispose();
            serviceB.Dispose();
            rogue.Dispose();
            rogueService.Dispose();
        }
    }

    private static string[] ReadNames(string output)
    {
        var json = output.Split('\n').First(line => line.StartsWith("RESULT:", StringComparison.Ordinal))[7..];
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray().Select(item => item.GetProperty("Name").GetString()!)
            .OrderBy(static x => x, StringComparer.Ordinal).ToArray();
    }

    private sealed class StoredPeer : IDisposable
    {
        private readonly ServiceProvider _provider;
        internal StoredPeer(string directory, string id, ManagementPeerKind kind)
        {
            Directory = directory;
            Identity = new ManagementPeerIdentity(id, kind);
            var services = new ServiceCollection();
            services.AddCryptoKitFileStorage(options => options.DirectoryPath = directory);
            services.AddCryptoKitRsa(options => options.KeySize = 2048);
            services.AddApiKitManagementCryptoKit(options => options.AddCredential(
                id, kind, "rsa-" + id, "credential-" + id, createIfMissing: false));
            _provider = services.BuildServiceProvider();
        }
        internal string Directory { get; }
        internal ManagementPeerIdentity Identity { get; }

        internal async Task InitAsync()
        {
            await _provider.GetRequiredService<IRsaKeyProvider>().GetOrCreatePublicKeyAsync("rsa-" + Identity.PeerId);
            _ = await _provider.GetRequiredService<ICryptoKitManagementPublicCredentialProvider>().GetActiveAsync(Identity);
        }

        internal async Task TrustAsync(StoredPeer other)
        {
            var otherPublic = await other._provider.GetRequiredService<ICryptoKitManagementPublicCredentialProvider>()
                .GetActiveAsync(other.Identity);
            await _provider.GetRequiredService<ICryptoKitManagementTrustStore>()
                .TrustAsync(other.Identity, otherPublic.CredentialId, otherPublic.PublicKeySubjectPublicKeyInfo);
        }

        public void Dispose() => _provider.Dispose();
    }

    private sealed class TestDirectories : IDisposable
    {
        private readonly string _root = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "apikit-multiprocess-" + Guid.NewGuid().ToString("N"));
        internal string Path(string leaf)
        {
            var result = System.IO.Path.Combine(_root, leaf);
            Directory.CreateDirectory(result);
            return result;
        }
        public void Dispose()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class WorkerProcess : IDisposable
    {
        private readonly Process _process;
        private bool _stopped;
        private WorkerProcess(Process process) => _process = process;

        internal static async Task<WorkerProcess> StartServerAsync(
            string role, WorkerSettings settings, TestDirectories dirs)
        {
            var process = Start(role, settings, dirs);
            try
            {
                var ready = await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
                if (ready != "READY")
                {
                    var error = await process.StandardError.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(5));
                    throw new InvalidOperationException($"Worker '{role}' did not start: {ready} {error}");
                }
                return new WorkerProcess(process);
            }
            catch
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                process.Dispose();
                throw;
            }
        }

        internal static async Task<(int ExitCode, string Output)> QueryAdminAsync(
            WorkerSettings settings, TestDirectories dirs)
        {
            using var process = Start("admin", settings, dirs);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            try
            {
                var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
                await process.WaitForExitAsync(timeout.Token);
                return (process.ExitCode, await outputTask);
            }
            finally
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
        }

        private static Process Start(string role, WorkerSettings settings, TestDirectories dirs)
        {
            var workerDll = FindWorkerDll();
            var settingsFile = System.IO.Path.Combine(dirs.Path("configs"), Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(settingsFile, JsonSerializer.Serialize(settings));
            var info = new ProcessStartInfo("dotnet")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            info.ArgumentList.Add(workerDll);
            info.ArgumentList.Add(role);
            info.ArgumentList.Add(settingsFile);
            return Process.Start(info) ?? throw new InvalidOperationException("Cannot start dotnet worker.");
        }

        private static string FindWorkerDll()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            var configuration = dir.Parent!.Name; // test bin/{Debug|Release}/net10.0-windows
            while (dir is not null && !File.Exists(System.IO.Path.Combine(dir.FullName, "ApiKit.slnx")))
            {
                dir = dir.Parent;
            }
            if (dir is null) throw new DirectoryNotFoundException("ApiKit.slnx was not found.");
            var dll = System.IO.Path.Combine(dir.FullName,
                "ApiKit.Management.Windows.TestWorker", "bin", configuration,
                "net10.0-windows", "ApiKit.Management.Windows.TestWorker.dll");
            if (!File.Exists(dll)) throw new FileNotFoundException("Build solution before multiprocess tests.", dll);
            return dll;
        }

        internal async Task StopAsync()
        {
            if (_stopped) return;
            _stopped = true;
            if (_process.HasExited) return;
            await _process.StandardInput.WriteLineAsync("STOP");
            await _process.StandardInput.FlushAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try { await _process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { _process.Kill(entireProcessTree: true); }
        }

        public void Dispose()
        {
            if (!_process.HasExited) _process.Kill(entireProcessTree: true);
            _process.Dispose();
        }
    }
}
