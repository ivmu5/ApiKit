namespace ApiKit.Management.Models;

/// <summary>
/// Defines the management or application behavior of management service registration.
/// </summary>
/// <param name="Descriptor">The descriptor value.</param>
/// <param name="LeaseDuration">The lease duration value.</param>
public sealed record ManagementServiceRegistration(
    ManagementServiceDescriptor Descriptor,
    TimeSpan LeaseDuration);
