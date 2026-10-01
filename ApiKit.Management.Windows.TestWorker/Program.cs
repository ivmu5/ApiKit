using System.Text.Json;
using ApiKit.Management;
using ApiKit.Management.Abstractions;
using ApiKit.Management.CryptoKit.DependencyInjection;
using ApiKit.Management.Security;
using ApiKit.Management.Windows;
using CryptoKit.Rsa;
using CryptoKit.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiKit.Management.Windows.TestWorker;

/// <summary>
/// Provides the test worker application entry point.
/// </summary>
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "scm")
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders();
            builder.Services.AddWindowsService(options => options.ServiceName = args[1]);
            await builder.Build().RunAsync();
            return 0;
        }

        if (args.Length != 2 || args[0] is not ("host" or "service" or "admin"))
        {
            Console.Error.WriteLine("Usage: TestWorker <host|service|admin> <settings.json>");
            return 2;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<WorkerSettings>(await File.ReadAllTextAsync(args[1]))
                ?? throw new InvalidDataException("Settings JSON is null.");
            var role = args[0];
            var builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders();
            builder.Services.AddRouting();
            builder.Services.AddCryptoKitFileStorage(options => options.DirectoryPath = settings.StorageDirectory);
            builder.Services.AddCryptoKitRsa(options => options.KeySize = 2048);
            builder.Services.AddApiKitManagementCryptoKit(options => options.AddCredential(
                settings.PeerId,
                role switch
                {
                    "host" => ManagementPeerKind.ManagementHost,
                    "service" => ManagementPeerKind.ManagedService,
                    _ => ManagementPeerKind.AdminClient
                },
                "rsa-" + settings.PeerId,
                "credential-" + settings.PeerId,
                createIfMissing: false));

            switch (role)
            {
                case "host":
                    builder.Services.AddApiKitWindowsManagementHost(
                        options =>
                        {
                            options.ManagementHostPeerId = settings.HostPeerId;
                            options.RegistrationPipeName = settings.RegistrationPipeName;
                            options.AdministrationPipeName = settings.AdminPipeName;
                            options.UseProvisionedServiceSidAcl = false;
                        },
                        options =>
                        {
                            options.EnableServiceSidIsolation = false;
                            options.ProtectManagedDirectories = false;
                            options.InstallRoot = settings.InstallRoot;
                        });
                    break;

                case "service":
                    builder.Services.AddApiKitManagement(options =>
                    {
                        options.ServiceName = settings.PeerId;
                        options.InstanceId = settings.InstanceId;
                        options.DisplayName = settings.PeerId;
                        options.DiscoverServerAddresses = false;
                        options.DiscoverEndpoints = false;
                        options.DiscoverCrudResources = false;
                        options.HeartbeatInterval = TimeSpan.FromSeconds(1);
                        options.LeaseDuration = TimeSpan.FromSeconds(10);
                    }).AddWindowsNamedPipeTransport(options =>
                    {
                        options.ManagementHostPeerId = settings.HostPeerId;
                        options.RegistrationPipeName = settings.RegistrationPipeName;
                        options.ManagementPipePrefix = settings.ServicePipePrefix;
                        options.UseProvisionedWindowsIsolation = false;
                    });
                    break;

                case "admin":
                    builder.Services.AddApiKitWindowsManagementClient(options =>
                    {
                        options.AdminClientPeerId = settings.PeerId;
                        options.ManagementHostPeerId = settings.HostPeerId;
                        options.AdministrationPipeName = settings.AdminPipeName;
                        options.ConnectTimeout = TimeSpan.FromSeconds(3);
                    });
                    break;
            }

            using var host = builder.Build();
            if (role == "admin")
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var services = await host.Services.GetRequiredService<IManagementServiceCatalog>()
                    .GetServicesAsync(timeout.Token);
                Console.WriteLine("RESULT:" + JsonSerializer.Serialize(services.Select(service => new
                {
                    Name = service.Descriptor.Identity.ServiceName,
                    InstanceId = service.Descriptor.Identity.InstanceId
                })));
                return 0;
            }

            await host.StartAsync();
            Console.WriteLine("READY");
            Console.Out.Flush();
            while (await Console.In.ReadLineAsync() is { } command && command != "STOP")
            {
            }
            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await host.StopAsync(stopTimeout.Token);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.GetType().Name + ": " + error.Message);
            return 1;
        }
    }
}

/// <summary>Test-only, non-secret process configuration. Credentials are stored separately.</summary>
public sealed class WorkerSettings
{
    /// <summary>Directory containing the test worker credential storage.</summary>
    public required string StorageDirectory { get; set; }
    /// <summary>Logical identity of the test worker.</summary>
    public required string PeerId { get; set; }
    /// <summary>Expected identity of the management host.</summary>
    public required string HostPeerId { get; set; }
    /// <summary>Named pipe used for service registration.</summary>
    public required string RegistrationPipeName { get; set; }
    /// <summary>Named pipe used for administrative requests.</summary>
    public required string AdminPipeName { get; set; }
    /// <summary>Prefix applied to service transport pipe names.</summary>
    public required string ServicePipePrefix { get; set; }
    /// <summary>Installation root directory for service packages.</summary>
    public required string InstallRoot { get; set; }
    /// <summary>Optional instance identifier used to distinguish concurrent test workers.</summary>
    public string? InstanceId { get; set; }
}
