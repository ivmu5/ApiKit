namespace ApiKit.Management.Models;

/// <summary>
/// Описывает сериализуемое свойство management-модели.
/// </summary>
public sealed record ManagementPropertyDescriptor
{
    /// <summary>CLR-имя свойства.</summary>
    public required string Name { get; init; }

    /// <summary>Имя свойства в JSON с учётом System.Text.Json configuration.</summary>
    public required string JsonName { get; init; }

    /// <summary>Полное CLR-имя типа свойства.</summary>
    public required string TypeName { get; init; }

    /// <summary>Указывает, допускает ли свойство null.</summary>
    public required bool IsNullable { get; init; }

    /// <summary>Указывает, обязательно ли значение модели.</summary>
    public required bool IsRequired { get; init; }

    /// <summary>Указывает, отсутствует ли публичный setter.</summary>
    public required bool IsReadOnly { get; init; }

    /// <summary>Указывает, является ли свойство коллекцией.</summary>
    public required bool IsCollection { get; init; }

    /// <summary>Допустимые текстовые значения enum, если тип свойства является enum.</summary>
    public IReadOnlyList<string> AllowedValues { get; init; } = [];
}
