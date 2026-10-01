namespace ApiKit.Management.Models;

/// <summary>
/// Defines the available states for management resource operation.
/// </summary>
public enum ManagementResourceOperation
{
    /// <summary>
    /// Represents the list resource operation.
    /// </summary>
    List,

    /// <summary>
    /// Represents the get resource operation.
    /// </summary>
    Get,

    /// <summary>
    /// Represents the create resource operation.
    /// </summary>
    Create,

    /// <summary>
    /// Represents the update resource operation.
    /// </summary>
    Update,

    /// <summary>
    /// Represents the patch resource operation.
    /// </summary>
    Patch,

    /// <summary>
    /// Defines the management or application behavior of management resource operation.
    /// </summary>
    Delete
}
