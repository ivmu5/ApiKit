using System.Text.Json;
using ApiKit.Authorization.Permissions;

namespace ApiKit.Authorization.Options;

/// <summary>
/// Defines configuration settings for ApiKit authorization options.
/// </summary>
public sealed class ApiKitAuthorizationOptions
{
    private readonly Dictionary<Type, string> _resourceNames = [];
    private readonly Dictionary<PermissionKey, string> _permissionNames = [];

    /// <summary>
    /// Gets or sets permission claim type.
    /// </summary>
    public string PermissionClaimType { get; set; } = PermissionAuthorizationDefaults.ClaimType;

    internal IReadOnlyDictionary<Type, string> ResourceNames => _resourceNames;

    internal IReadOnlyDictionary<PermissionKey, string> PermissionNames => _permissionNames;

    /// <summary>
    /// Configures a mapping for resource.
    /// </summary>
    /// <typeparam name="TResource">The t resource type.</typeparam>
    /// <param name="resourceName">The management resource name.</param>
    /// <returns>The result of the operation.</returns>
    public ApiKitAuthorizationOptions MapResource<TResource>(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        _resourceNames[typeof(TResource)] = resourceName.Trim();
        return this;
    }

    /// <summary>
    /// Configures a mapping for permission.
    /// </summary>
    /// <typeparam name="TResource">The t resource type.</typeparam>
    /// <param name="operation">The requested operation.</param>
    /// <param name="permissionName">The permission name value.</param>
    /// <returns>The result of the operation.</returns>
    public ApiKitAuthorizationOptions MapPermission<TResource>(
        PermissionOperation operation,
        string permissionName)
    {
        return MapPermission<TResource>(operation.ToString(), permissionName);
    }

    /// <summary>
    /// Configures a mapping for permission.
    /// </summary>
    /// <typeparam name="TResource">The t resource type.</typeparam>
    /// <param name="operation">The requested operation.</param>
    /// <param name="permissionName">The permission name value.</param>
    /// <returns>The result of the operation.</returns>
    public ApiKitAuthorizationOptions MapPermission<TResource>(
        string operation,
        string permissionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);

        _permissionNames[new PermissionKey(typeof(TResource), NormalizeOperation(operation))] =
            permissionName.Trim();

        return this;
    }

    private static string NormalizeOperation(string operation) =>
        JsonNamingPolicy.KebabCaseLower.ConvertName(operation.Trim());

    internal readonly record struct PermissionKey(Type ResourceType, string Operation);
}
