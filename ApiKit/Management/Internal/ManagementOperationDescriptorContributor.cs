using ApiKit.Management.Abstractions;
using ApiKit.Management.Builders;

namespace ApiKit.Management.Internal;

internal sealed class ManagementOperationDescriptorContributor(
    IEnumerable<IManagementOperationRegistration> registrations)
    : IManagementDescriptorContributor
{
    public ValueTask ContributeAsync(
        ManagementServiceDescriptorBuilder builder,
        CancellationToken cancellationToken = default)
    {
        var count = 0;

        foreach (var registration in registrations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.AddOperation(registration.Descriptor);
            count++;
        }

        if (count > 0)
        {
            builder.AddCapability(ManagementCapabilities.Operations);
        }

        return ValueTask.CompletedTask;
    }
}
