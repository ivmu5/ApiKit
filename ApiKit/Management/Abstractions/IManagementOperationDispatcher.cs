using System.Security.Claims;
using System.Text.Json;
using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Выполняет зарегистрированные management-операции по техническому имени.
/// </summary>
public interface IManagementOperationDispatcher
{
    /// <summary>
    /// Выполняет операцию с предварительной проверкой стандартной ASP.NET Core authorization policy.
    /// </summary>
    /// <param name="operationName">Техническое имя операции.</param>
    /// <param name="request">JSON-представление входной модели.</param>
    /// <param name="principal">Аутентифицированный management-клиент.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Унифицированный результат выполнения.</returns>
    ValueTask<ManagementOperationExecutionResult> ExecuteAsync(
        string operationName,
        JsonElement? request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
