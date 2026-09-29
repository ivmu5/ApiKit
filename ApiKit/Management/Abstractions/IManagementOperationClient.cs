using System.Text.Json;
using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Клиентская абстракция для вызова management-операций конкретного экземпляра сервиса.
/// </summary>
/// <remarks>
/// Реализация админ-панели может использовать этот контракт независимо от IPC-технологии.
/// </remarks>
public interface IManagementOperationClient
{
    /// <summary>
    /// Выполняет management-операцию на удалённом экземпляре сервиса.
    /// </summary>
    ValueTask<ManagementOperationExecutionResult> ExecuteAsync(
        string instanceId,
        string operationName,
        JsonElement? request,
        CancellationToken cancellationToken = default);
}
