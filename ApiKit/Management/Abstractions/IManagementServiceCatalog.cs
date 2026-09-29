using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Read-only каталог обнаруженных управляемых сервисов.
/// </summary>
/// <remarks>
/// Интерфейс подходит как для локального management host, так и для клиента админ-панели,
/// использующего транспортный адаптер.
/// </remarks>
public interface IManagementServiceCatalog
{
    /// <summary>Возвращает все актуальные экземпляры сервисов.</summary>
    ValueTask<IReadOnlyList<RegisteredManagementService>> GetServicesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Возвращает все актуальные экземпляры указанного сервиса.</summary>
    ValueTask<IReadOnlyList<RegisteredManagementService>> GetServicesAsync(
        string serviceName,
        CancellationToken cancellationToken = default);

    /// <summary>Возвращает экземпляр по instance id или <see langword="null"/>.</summary>
    ValueTask<RegisteredManagementService?> FindAsync(
        string instanceId,
        CancellationToken cancellationToken = default);
}
