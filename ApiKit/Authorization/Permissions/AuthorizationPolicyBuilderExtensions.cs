using Microsoft.AspNetCore.Authorization;

namespace ApiKit.Authorization.Permissions;

/// <summary>
/// Defines extension methods for authorization policy builder extensions.
/// </summary>
public static class AuthorizationPolicyBuilderExtensions
{
    /// <summary>
    /// Adds an ASP.NET Core authorization requirement for the specified permission.
    /// </summary>
    /// <param name="builder">The service or endpoint builder.</param>
    /// <param name="permission">The permission value.</param>
    /// <param name="claimType">The claim type value.</param>
    /// <returns>The result of the operation.</returns>
    public static AuthorizationPolicyBuilder RequirePermission(
        this AuthorizationPolicyBuilder builder,
        string permission,
        string claimType = PermissionAuthorizationDefaults.ClaimType)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        // PermissionRequirement/PermissionHandler.
        return builder
            .RequireAuthenticatedUser()
            .RequireClaim(claimType, permission);
    }
}
