namespace ApiKit.Management.Models;

/// <summary>
/// Describes a CRUD resource discovered for management operations.
/// </summary>
public sealed record ManagementResourceDescriptor
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets or sets group name.
    /// </summary>
    public string? GroupName { get; init; }

    /// <summary>
    /// Gets or sets controller name.
    /// </summary>
    public required string ControllerName { get; init; }

    /// <summary>
    /// Gets or sets entity type name.
    /// </summary>
    public required string EntityTypeName { get; init; }

    /// <summary>
    /// Gets or sets key type name.
    /// </summary>
    public required string KeyTypeName { get; init; }

    /// <summary>
    /// Gets or sets key JSON name.
    /// </summary>
    public string? KeyJsonName { get; init; }

    /// <summary>
    /// Gets or sets read model.
    /// </summary>
    public required ManagementModelDescriptor ReadModel { get; init; }

    /// <summary>
    /// Gets or sets create model.
    /// </summary>
    public required ManagementModelDescriptor CreateModel { get; init; }

    /// <summary>
    /// Gets or sets update model.
    /// </summary>
    public required ManagementModelDescriptor UpdateModel { get; init; }

    /// <summary>
    /// Gets or sets operations.
    /// </summary>
    public required IReadOnlyList<ManagementResourceOperation> Operations { get; init; }

    /// <summary>
    /// Gets or sets permissions.
    /// </summary>
    public IReadOnlyDictionary<ManagementResourceOperation, string> Permissions { get; init; } =
        new Dictionary<ManagementResourceOperation, string>();
}
