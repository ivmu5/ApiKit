using System.Collections.Concurrent;
using ApiKit.Management.Abstractions;
using ApiKit.Management.Models;

namespace ApiKit.Management.Internal;

internal sealed class InMemoryManagementServiceRegistry(TimeProvider timeProvider)
    : IManagementServiceRegistry
{
    private readonly ConcurrentDictionary<string, RegisteredManagementService> _services =
        new(StringComparer.OrdinalIgnoreCase);

    public ValueTask UpsertAsync(
        ManagementServiceRegistration registration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        cancellationToken.ThrowIfCancellationRequested();

        if (registration.LeaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(registration),
                "LeaseDuration должен быть больше нуля.");
        }

        var now = timeProvider.GetUtcNow();
        var instanceId = registration.Descriptor.Identity.InstanceId;

        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(registration.Descriptor.Identity.ServiceName);

        _services[instanceId] = new RegisteredManagementService(
            registration.Descriptor,
            now,
            now + registration.LeaseDuration);

        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> RemoveAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_services.TryRemove(instanceId, out _));
    }

    public ValueTask<IReadOnlyList<RegisteredManagementService>> GetServicesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RemoveExpired();

        IReadOnlyList<RegisteredManagementService> result = _services.Values
            .OrderBy(service => service.Descriptor.Identity.ServiceName)
            .ThenBy(service => service.Descriptor.Identity.InstanceId)
            .ToArray();

        return ValueTask.FromResult(result);
    }

    public ValueTask<IReadOnlyList<RegisteredManagementService>> GetServicesAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        cancellationToken.ThrowIfCancellationRequested();
        RemoveExpired();

        IReadOnlyList<RegisteredManagementService> result = _services.Values
            .Where(service => string.Equals(
                service.Descriptor.Identity.ServiceName,
                serviceName,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(service => service.Descriptor.Identity.InstanceId)
            .ToArray();

        return ValueTask.FromResult(result);
    }

    public ValueTask<RegisteredManagementService?> FindAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        cancellationToken.ThrowIfCancellationRequested();
        RemoveExpired();

        _services.TryGetValue(instanceId, out var service);
        return ValueTask.FromResult(service);
    }

    private void RemoveExpired()
    {
        var now = timeProvider.GetUtcNow();

        foreach (var pair in _services)
        {
            if (pair.Value.ExpiresAtUtc <= now)
            {
                _services.TryRemove(pair.Key, out _);
            }
        }
    }
}
