using ApiKit.Management.Abstractions;
using ApiKit.Management.Builders;
using ApiKit.Management.Models;
using ApiKit.Management.Windows.Options;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Windows.Internal;

internal sealed class WindowsNamedPipeTransportDescriptorContributor(
    IManagementServiceIdentityProvider identityProvider,
    IOptions<WindowsManagementServiceOptions> options)
    : IManagementDescriptorContributor
{
    public ValueTask ContributeAsync(
        ManagementServiceDescriptorBuilder builder,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var pipeName = WindowsNamedPipeNames.GetServicePipeName(
            options.Value.ManagementPipePrefix,
            identityProvider.GetIdentity());

        builder.AddTransport(new ManagementTransportDescriptor(
            WindowsManagementTransportDefaults.TransportName,
            pipeName)
        {
            Purpose = WindowsManagementTransportDefaults.ManagementPurpose
        });

        builder.AddCapability("management.named-pipe");
        return ValueTask.CompletedTask;
    }
}
