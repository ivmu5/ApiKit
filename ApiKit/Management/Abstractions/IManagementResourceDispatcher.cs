using System.Security.Claims;
using System.Text.Json;
using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Выполняет management-запросы к CRUD-ресурсам внутри текущего ASP.NET Core сервиса.
/// </summary>
/// <remarks>
/// Реализация должна сохранять штатный MVC pipeline ресурса: model binding, validation,
/// authorization filters, переопределённые actions и формирование HTTP-результатов.
/// Transport-модули используют этот интерфейс как локальную границу сервиса.
/// </remarks>
public interface IManagementResourceDispatcher
{
    /// <summary>Выполняет стандартную операцию над CRUD-ресурсом.</summary>
    /// <param name="resourceName">Стабильное management-имя ресурса.</param>
    /// <param name="operation">Операция над ресурсом.</param>
    /// <param name="principal">Аутентифицированный management principal.</param>
    /// <param name="key">Сериализованный ключ ресурса, если он требуется.</param>
    /// <param name="model">Сериализованная входная модель или JSON Patch документ.</param>
    /// <param name="page">Номер страницы для <see cref="ManagementResourceOperation.List"/>.</param>
    /// <param name="pageSize">Размер страницы для <see cref="ManagementResourceOperation.List"/>.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    ValueTask<ManagementResourceExecutionResult> ExecuteAsync(
        string resourceName,
        ManagementResourceOperation operation,
        ClaimsPrincipal principal,
        JsonElement? key = null,
        JsonElement? model = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);
}
