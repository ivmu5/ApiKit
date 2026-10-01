using ApiKit.Management.Models;

namespace ApiKit.Management.Options;

/// <summary>
/// Defines configuration settings for management resource options.
/// </summary>
public sealed class ManagementResourceOptions
{
    /// <summary>
    /// Gets or sets whether enabled is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets group name.
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// Gets or sets key JSON name.
    /// </summary>
    public string? KeyJsonName { get; set; }

    /// <summary>
    /// Gets or sets allowed operations.
    /// </summary>
    public ISet<ManagementResourceOperation>? AllowedOperations { get; set; }

    /// <summary>
    /// Limits the resource to the explicitly allowed management operations.
    /// </summary>
    public ManagementResourceOptions AllowOnly(params ManagementResourceOperation[] operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        AllowedOperations = new HashSet<ManagementResourceOperation>(operations);
        return this;
    }
}
