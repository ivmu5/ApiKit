namespace ApiKit.Authorization.Permissions;

/// <summary>
/// Defines the contract for i permission name resolver.
/// </summary>
public interface IPermissionNameResolver
{
    /// <summary>
    /// Returns permission.
    /// </summary>
    /// <typeparam name="TResource">The t resource type.</typeparam>
    /// <param name="operation">The requested operation.</param>
    /// <returns>The requested value.</returns>
    string GetPermission<TResource>(PermissionOperation operation);

    /// <summary>
    /// Returns permission.
    /// </summary>
    /// <typeparam name="TResource">The t resource type.</typeparam>
    /// <param name="operation">The requested operation.</param>
    /// <returns>The requested value.</returns>
    string GetPermission<TResource>(string operation);

    /// <summary>
    /// Returns permission.
    /// </summary>
    /// <param name="resourceType">The resource type value.</param>
    /// <param name="operation">The requested operation.</param>
    /// <returns>The requested value.</returns>
    string GetPermission(Type resourceType, string operation);
}
