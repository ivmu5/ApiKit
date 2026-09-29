namespace ApiKit.Authorization.Permissions;

/// <summary>
/// Определяет стандартные операции над API-ресурсом,
/// для которых ApiKit может сформировать permission автоматически.
/// </summary>
public enum PermissionOperation
{
    /// <summary>
    /// Чтение ресурса.
    /// </summary>
    Read,

    /// <summary>
    /// Создание ресурса.
    /// </summary>
    Create,

    /// <summary>
    /// Изменение ресурса.
    /// </summary>
    Update,

    /// <summary>
    /// Удаление ресурса.
    /// </summary>
    Delete
}
