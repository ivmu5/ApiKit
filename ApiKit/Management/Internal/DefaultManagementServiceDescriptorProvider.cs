using ApiKit.Management.Abstractions;
using ApiKit.Management.Builders;
using ApiKit.Management.Models;
using ApiKit.Management.Options;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Internal;

internal sealed class DefaultManagementServiceDescriptorProvider(
    IManagementServiceIdentityProvider identityProvider,
    IEnumerable<IManagementDescriptorContributor> contributors,
    IOptions<ApiKitManagementOptions> options)
    : IManagementServiceDescriptorProvider
{
    public async ValueTask<ManagementServiceDescriptor> GetDescriptorAsync(
        CancellationToken cancellationToken = default)
    {
        var builder = new ManagementServiceDescriptorBuilder(identityProvider.GetIdentity());

        foreach (var capability in options.Value.Capabilities)
        {
            builder.AddCapability(capability);
        }

        foreach (var pair in options.Value.Metadata)
        {
            builder.SetMetadata(pair.Key, pair.Value);
        }

        foreach (var contributor in contributors)
        {
            await contributor.ContributeAsync(builder, cancellationToken);
        }

        return builder.Build();
    }
}
