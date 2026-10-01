namespace ApiKit.Authorization.Permissions;

/// <summary>
/// Defines the available states for permission operation.
/// </summary>
public enum PermissionOperation
{
    /// <summary>
    /// Represents the read permission operation.
    /// </summary>
    Read,

    /// <summary>
    /// Represents the create permission operation.
    /// </summary>
    Create,

    /// <summary>
    /// Represents the update permission operation.
    /// </summary>
    Update,

    /// <summary>
    /// Defines the management or application behavior of permission operation.
    /// </summary>
    Delete
}
