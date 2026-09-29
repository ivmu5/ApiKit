using ApiKit.Management.Abstractions;
using ApiKit.Management.Builders;
using ApiKit.Management.Options;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Internal;

internal sealed class AspNetCoreServerAddressContributor(
    IServiceProvider serviceProvider,
    IOptions<ApiKitManagementOptions> options)
    : IManagementDescriptorContributor
{
    public ValueTask ContributeAsync(
        ManagementServiceDescriptorBuilder builder,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.DiscoverServerAddresses)
        {
            return ValueTask.CompletedTask;
        }

        var server = serviceProvider.GetService<IServer>();
        var addresses = server?.Features.Get<IServerAddressesFeature>()?.Addresses;

        if (addresses is null || addresses.Count == 0)
        {
            return ValueTask.CompletedTask;
        }

        foreach (var address in addresses)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.AddAddress(address);
        }

        builder.AddCapability(ManagementCapabilities.Addresses);
        return ValueTask.CompletedTask;
    }
}
