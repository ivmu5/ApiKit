using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;

namespace ApiKit.Management.Internal;

internal sealed class InMemoryManagementServicePublisher(
    IManagementServiceRegistry registry)
    : IManagementServicePublisher
{
    public ValueTask PublishAsync(
        ManagementServiceRegistration registration,
        CancellationToken cancellationToken = default) =>
        registry.UpsertAsync(registration, cancellationToken);

    public async ValueTask WithdrawAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        await registry.RemoveAsync(instanceId, cancellationToken);
    }
}
