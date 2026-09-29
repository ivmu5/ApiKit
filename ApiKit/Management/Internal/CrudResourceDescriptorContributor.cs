using ApiKit.Management.Abstractions;
using ApiKit.Management.Builders;

namespace ApiKit.Management.Internal;

internal sealed class CrudResourceDescriptorContributor(
    CrudManagementResourceRegistry registry)
    : IManagementDescriptorContributor
{
    public ValueTask ContributeAsync(
        ManagementServiceDescriptorBuilder builder,
        CancellationToken cancellationToken = default)
    {
        var resources = registry.GetResources();

        foreach (var resource in resources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.AddResource(resource.Descriptor);
        }

        if (resources.Count > 0)
        {
            builder.AddCapability(ManagementCapabilities.Resources);
        }

        return ValueTask.CompletedTask;
    }
}
