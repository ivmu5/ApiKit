using System.Reflection;
using System.Text.Json;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;
using ApiKit.Management.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Internal;

internal sealed class ManagementServiceIdentityProvider : IManagementServiceIdentityProvider
{
    private readonly ManagementServiceIdentity _identity;

    public ManagementServiceIdentityProvider(
        IOptions<ApiKitManagementOptions> options,
        IHostEnvironment environment,
        TimeProvider timeProvider)
    {
        var settings = options.Value;
        var entryAssembly = Assembly.GetEntryAssembly();
        var applicationName = entryAssembly?.GetName().Name
            ?? environment.ApplicationName
            ?? "service";

        var serviceName = string.IsNullOrWhiteSpace(settings.ServiceName)
            ? JsonNamingPolicy.KebabCaseLower.ConvertName(applicationName)
            : settings.ServiceName.Trim();

        var displayName = string.IsNullOrWhiteSpace(settings.DisplayName)
            ? applicationName
            : settings.DisplayName.Trim();

        var instanceId = string.IsNullOrWhiteSpace(settings.InstanceId)
            ? Guid.NewGuid().ToString("N")
            : settings.InstanceId.Trim();

        var version = string.IsNullOrWhiteSpace(settings.Version)
            ? GetAssemblyVersion(entryAssembly)
            : settings.Version.Trim();

        _identity = new ManagementServiceIdentity(
            serviceName,
            displayName,
            instanceId,
            version,
            environment.EnvironmentName,
            Environment.MachineName,
            Environment.ProcessId,
            timeProvider.GetUtcNow());
    }

    public ManagementServiceIdentity GetIdentity() => _identity;

    private static string? GetAssemblyVersion(Assembly? assembly)
    {
        if (assembly is null)
        {
            return null;
        }

        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                   ?.InformationalVersion
               ?? assembly.GetName().Version?.ToString();
    }
}
