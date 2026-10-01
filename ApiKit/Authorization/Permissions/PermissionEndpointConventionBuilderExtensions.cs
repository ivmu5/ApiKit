using Microsoft.AspNetCore.Builder;

namespace ApiKit.Authorization.Permissions;

/// <summary>
/// Defines extension methods for permission endpoint convention builder extensions.
/// </summary>
public static class PermissionEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds an ASP.NET Core authorization requirement for the specified permission.
    /// </summary>
    /// <typeparam name="TResource">The t resource type.</typeparam>
    /// <param name="builder">The service or endpoint builder.</param>
    /// <param name="operation">The requested operation.</param>
    /// <returns>The result of the operation.</returns>
    public static IEndpointConventionBuilder RequirePermission<TResource>(
        this IEndpointConventionBuilder builder,
        PermissionOperation operation)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(
            PermissionPolicyName.ForResource(typeof(TResource), operation));
    }

    /// <summary>
    /// Adds an ASP.NET Core authorization requirement for the specified permission.
    /// </summary>
    /// <typeparam name="TResource">The t resource type.</typeparam>
    /// <param name="builder">The service or endpoint builder.</param>
    /// <param name="operation">The requested operation.</param>
    /// <returns>The result of the operation.</returns>
    public static IEndpointConventionBuilder RequirePermission<TResource>(
        this IEndpointConventionBuilder builder,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(
            PermissionPolicyName.ForResource(typeof(TResource), operation));
    }

    /// <summary>
    /// Adds an ASP.NET Core authorization requirement for the specified permission.
    /// </summary>
    /// <param name="builder">The service or endpoint builder.</param>
    /// <param name="permission">The permission value.</param>
    /// <returns>The result of the operation.</returns>
    public static IEndpointConventionBuilder RequirePermission(
        this IEndpointConventionBuilder builder,
        string permission)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(
            PermissionPolicyName.ForPermission(permission));
    }
}
