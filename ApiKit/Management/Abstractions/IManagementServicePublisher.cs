using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Defines the contract for i management service publisher.
/// </summary>
public interface IManagementServicePublisher
{
    /// <summary>
    /// Publishes async.
    /// </summary>
    ValueTask PublishAsync(
        ManagementServiceRegistration registration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Withdraws the previously published service registration.
    /// </summary>
    ValueTask WithdrawAsync(
        string instanceId,
        CancellationToken cancellationToken = default);
}
