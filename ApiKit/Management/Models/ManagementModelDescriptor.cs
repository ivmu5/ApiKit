namespace ApiKit.Management.Models;

/// <summary>
/// Describes the shape of a model exposed by a management resource.
/// </summary>
public sealed record ManagementModelDescriptor
{
    /// <summary>
    /// Gets or sets type name.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets or sets properties.
    /// </summary>
    public required IReadOnlyList<ManagementPropertyDescriptor> Properties { get; init; }
}
