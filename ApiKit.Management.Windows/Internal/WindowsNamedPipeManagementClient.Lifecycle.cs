using ApiKit.Management.Lifecycle;

namespace ApiKit.Management.Windows.Internal;

internal sealed partial class WindowsNamedPipeManagementClient
{
    public async ValueTask<ManagedServiceState> GetStateAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        var response = await SendAsync(
            WindowsNamedPipeRequestTypes.GetServiceState,
            new ManagedServiceNameRequest(serviceName),
            cancellationToken);

        return ReadPayload<ManagedServiceState>(response);
    }

    public async ValueTask StartAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        await SendAsync(
            WindowsNamedPipeRequestTypes.StartService,
            new ManagedServiceNameRequest(serviceName),
            cancellationToken);
    }

    public async ValueTask StopAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        await SendAsync(
            WindowsNamedPipeRequestTypes.StopService,
            new ManagedServiceNameRequest(serviceName),
            cancellationToken);
    }

    public async ValueTask RestartAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        await SendAsync(
            WindowsNamedPipeRequestTypes.RestartService,
            new ManagedServiceNameRequest(serviceName),
            cancellationToken);
    }

    public async ValueTask InstallAsync(
        ManagedServicePackageManifest manifest,
        string packageDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageDirectory);
        await SendAsync(
            WindowsNamedPipeRequestTypes.InstallService,
            new ManagedServicePackageRequest(manifest, packageDirectory),
            cancellationToken);
    }

    public async ValueTask UpdateAsync(
        ManagedServicePackageManifest manifest,
        string packageDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageDirectory);
        await SendAsync(
            WindowsNamedPipeRequestTypes.UpdateService,
            new ManagedServicePackageRequest(manifest, packageDirectory),
            cancellationToken);
    }

    public async ValueTask UninstallAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        await SendAsync(
            WindowsNamedPipeRequestTypes.UninstallService,
            new ManagedServiceNameRequest(serviceName),
            cancellationToken);
    }
}
