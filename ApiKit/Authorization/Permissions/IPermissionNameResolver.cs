namespace ApiKit.Authorization.Permissions;

/// <summary>
/// Формирует итоговые имена permissions по типу ресурса и операции.
/// </summary>
public interface IPermissionNameResolver
{
    /// <summary>
    /// Возвращает permission для стандартной операции над ресурсом.
    /// </summary>
    /// <typeparam name="TResource">Тип API-ресурса.</typeparam>
    /// <param name="operation">Операция над ресурсом.</param>
    /// <returns>Итоговое имя permission.</returns>
    string GetPermission<TResource>(PermissionOperation operation);

    /// <summary>
    /// Возвращает permission для произвольной операции над ресурсом.
    /// </summary>
    /// <typeparam name="TResource">Тип API-ресурса.</typeparam>
    /// <param name="operation">Имя операции.</param>
    /// <returns>Итоговое имя permission.</returns>
    string GetPermission<TResource>(string operation);

    /// <summary>
    /// Возвращает permission для указанного типа ресурса и операции.
    /// </summary>
    /// <param name="resourceType">Тип API-ресурса.</param>
    /// <param name="operation">Имя операции.</param>
    /// <returns>Итоговое имя permission.</returns>
    string GetPermission(Type resourceType, string operation);
}
