using System.Text.Json;
using ApiKit.Management.Models;

namespace ApiKit.Management.Abstractions;

/// <summary>
/// Клиент для унифицированного доступа админ-панели к CRUD-ресурсам управляемых сервисов.
/// </summary>
/// <remarks>
/// Интерфейс описывает transport-independent контракт. Конкретный сервис сам определяет,
/// каким образом management-запрос проходит через его штатный CRUD pipeline и validation.
/// </remarks>
public interface IManagementResourceClient
{
    /// <summary>Возвращает страницу ресурсов.</summary>
    ValueTask<ManagementResourcePage> ListAsync(
        string instanceId,
        string resourceName,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    /// <summary>Возвращает один ресурс по сериализованному ключу.</summary>
    ValueTask<JsonElement?> GetAsync(
        string instanceId,
        string resourceName,
        JsonElement key,
        CancellationToken cancellationToken = default);

    /// <summary>Создаёт ресурс из его management create-model.</summary>
    ValueTask<JsonElement> CreateAsync(
        string instanceId,
        string resourceName,
        JsonElement model,
        CancellationToken cancellationToken = default);

    /// <summary>Полностью обновляет ресурс.</summary>
    ValueTask UpdateAsync(
        string instanceId,
        string resourceName,
        JsonElement key,
        JsonElement model,
        CancellationToken cancellationToken = default);

    /// <summary>Частично обновляет ресурс стандартным JSON Patch документом.</summary>
    ValueTask<JsonElement> PatchAsync(
        string instanceId,
        string resourceName,
        JsonElement key,
        JsonElement patchDocument,
        CancellationToken cancellationToken = default);

    /// <summary>Удаляет ресурс.</summary>
    ValueTask DeleteAsync(
        string instanceId,
        string resourceName,
        JsonElement key,
        CancellationToken cancellationToken = default);
}
