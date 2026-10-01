namespace ApiKit.Management.Models;

/// <summary>
/// Defines the management or application behavior of registered management service.
/// </summary>
/// <param name="Descriptor">The descriptor value.</param>
/// <param name="LastSeenAtUtc">The last seen at UTC value.</param>
/// <param name="ExpiresAtUtc">The expires at UTC value.</param>
public sealed record RegisteredManagementService(
    ManagementServiceDescriptor Descriptor,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset ExpiresAtUtc);
