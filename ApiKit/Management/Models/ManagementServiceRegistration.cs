namespace ApiKit.Management.Models;

/// <summary>
/// Описывает регистрацию экземпляра сервиса в management registry.
/// </summary>
/// <param name="Descriptor">Актуальный descriptor сервиса.</param>
/// <param name="LeaseDuration">Срок действия регистрации без следующего heartbeat.</param>
public sealed record ManagementServiceRegistration(
    ManagementServiceDescriptor Descriptor,
    TimeSpan LeaseDuration);
