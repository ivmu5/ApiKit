namespace ApiKit.Management.Models;

/// <summary>
/// Описывает публичную JSON-модель CRUD-ресурса для динамического management UI.
/// </summary>
public sealed record ManagementModelDescriptor
{
    /// <summary>Полное CLR-имя модели.</summary>
    public required string TypeName { get; init; }

    /// <summary>Сериализуемые свойства верхнего уровня.</summary>
    public required IReadOnlyList<ManagementPropertyDescriptor> Properties { get; init; }
}
