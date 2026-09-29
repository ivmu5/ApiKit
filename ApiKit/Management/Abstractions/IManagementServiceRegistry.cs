using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Изменяемый реестр обнаруженных экземпляров управляемых сервисов.
/// </summary>
public interface IManagementServiceRegistry : IManagementServiceCatalog
{
    /// <summary>Добавляет или обновляет регистрацию экземпляра.</summary>
    ValueTask UpsertAsync(
        ManagementServiceRegistration registration,
        CancellationToken cancellationToken = default);

    /// <summary>Удаляет экземпляр по идентификатору.</summary>
    ValueTask<bool> RemoveAsync(
        string instanceId,
        CancellationToken cancellationToken = default);
}
