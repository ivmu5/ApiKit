namespace ApiKit.Management.Models;

/// <summary>
/// Describes a property of a management resource model.
/// </summary>
public sealed record ManagementPropertyDescriptor
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets JSON name.
    /// </summary>
    public required string JsonName { get; init; }

    /// <summary>
    /// Gets or sets type name.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets or sets whether is nullable is enabled.
    /// </summary>
    public required bool IsNullable { get; init; }

    /// <summary>
    /// Gets or sets whether is required is enabled.
    /// </summary>
    public required bool IsRequired { get; init; }

    /// <summary>
    /// Gets or sets whether is read only is enabled.
    /// </summary>
    public required bool IsReadOnly { get; init; }

    /// <summary>
    /// Gets or sets whether is collection is enabled.
    /// </summary>
    public required bool IsCollection { get; init; }

    /// <summary>
    /// Gets or sets allowed values.
    /// </summary>
    public IReadOnlyList<string> AllowedValues { get; init; } = [];
}
