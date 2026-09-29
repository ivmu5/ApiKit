namespace ApiKit.Management.Models;

/// <summary>
/// Тип стандартной операции над управляемым CRUD-ресурсом.
/// </summary>
public enum ManagementResourceOperation
{
    /// <summary>Получение списка ресурсов.</summary>
    List,

    /// <summary>Получение одного ресурса.</summary>
    Get,

    /// <summary>Создание ресурса.</summary>
    Create,

    /// <summary>Полное обновление ресурса.</summary>
    Update,

    /// <summary>Частичное обновление ресурса.</summary>
    Patch,

    /// <summary>Удаление ресурса.</summary>
    Delete
}
