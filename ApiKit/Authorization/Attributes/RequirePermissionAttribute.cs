using ApiKit.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace ApiKit.Authorization.Attributes;

/// <summary>
/// Defines the management or application behavior of require permission attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new RequirePermissionAttribute instance.
    /// </summary>
    /// <param name="permission">The permission value.</param>
    public RequirePermissionAttribute(string permission)
    {
        Policy = PermissionPolicyName.ForPermission(permission);
    }
}

/// <summary>
/// Defines the management or application behavior of require permission attribute.
/// </summary>
/// <typeparam name="TResource">The t resource type.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute<TResource> : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new RequirePermissionAttribute instance.
    /// </summary>
    /// <param name="operation">The requested operation.</param>
    public RequirePermissionAttribute(PermissionOperation operation)
    {
        Policy = PermissionPolicyName.ForResource(typeof(TResource), operation);
    }

    /// <summary>
    /// Initializes a new RequirePermissionAttribute instance.
    /// </summary>
    /// <param name="operation">The requested operation.</param>
    public RequirePermissionAttribute(string operation)
    {
        Policy = PermissionPolicyName.ForResource(typeof(TResource), operation);
    }
}
