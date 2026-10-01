namespace ApiKit.Management.Models;

/// <summary>
/// Defines the management or application behavior of management service identity.
/// </summary>
/// <param name="ServiceName">The service name value.</param>
/// <param name="DisplayName">The display name value.</param>
/// <param name="InstanceId">The instance ID value.</param>
/// <param name="Version">The version value.</param>
/// <param name="Environment">The environment value.</param>
/// <param name="MachineName">The machine name value.</param>
/// <param name="ProcessId">The process ID value.</param>
/// <param name="StartedAtUtc">The started at UTC value.</param>
public sealed record ManagementServiceIdentity(
    string ServiceName,
    string DisplayName,
    string InstanceId,
    string? Version,
    string Environment,
    string MachineName,
    int ProcessId,
    DateTimeOffset StartedAtUtc);
