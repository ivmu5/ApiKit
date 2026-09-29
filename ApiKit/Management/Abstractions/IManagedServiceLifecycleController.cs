using ApiKit.Management.Lifecycle;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Абстракция management host для управления жизненным циклом установленных сервисов.
/// </summary>
public interface IManagedServiceLifecycleController
{
    /// <summary>Возвращает состояние сервиса.</summary>
    ValueTask<ManagedServiceState> GetStateAsync(
        string serviceName,
        CancellationToken cancellationToken = default);

    /// <summary>Запускает сервис.</summary>
    ValueTask StartAsync(
        string serviceName,
        CancellationToken cancellationToken = default);

    /// <summary>Останавливает сервис.</summary>
    ValueTask StopAsync(
        string serviceName,
        CancellationToken cancellationToken = default);

    /// <summary>Перезапускает сервис.</summary>
    ValueTask RestartAsync(
        string serviceName,
        CancellationToken cancellationToken = default);
}
