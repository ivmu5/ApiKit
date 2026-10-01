namespace ApiKit.Management.Models;

/// <summary>
/// Describes an administrative operation, its input, and authorization requirements.
/// </summary>
public sealed record ManagementOperationDescriptor
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
    /// Gets or sets description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets group name.
    /// </summary>
    public string? GroupName { get; init; }

    /// <summary>
    /// Gets or sets request type name.
    /// </summary>
    public required string RequestTypeName { get; init; }

    /// <summary>
    /// Gets or sets result type name.
    /// </summary>
    public required string ResultTypeName { get; init; }

    /// <summary>
    /// Gets or sets authorization policy.
    /// </summary>
    public required string AuthorizationPolicy { get; init; }
}
