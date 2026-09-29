namespace ApiKit.Management.Models;

/// <summary>
/// Описывает запись о сервисе в management registry.
/// </summary>
/// <param name="Descriptor">Последний полученный descriptor.</param>
/// <param name="LastSeenAtUtc">Время последней регистрации или heartbeat.</param>
/// <param name="ExpiresAtUtc">Время истечения lease.</param>
public sealed record RegisteredManagementService(
    ManagementServiceDescriptor Descriptor,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset ExpiresAtUtc);
