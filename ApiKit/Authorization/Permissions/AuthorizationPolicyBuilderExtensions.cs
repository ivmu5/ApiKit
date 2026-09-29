using Microsoft.AspNetCore.Authorization;

namespace ApiKit.Authorization.Permissions;

/// <summary>
/// Содержит расширения стандартного <see cref="AuthorizationPolicyBuilder"/>
/// для permission-based authorization.
/// </summary>
public static class AuthorizationPolicyBuilderExtensions
{
    /// <summary>
    /// Добавляет в policy требование аутентифицированного пользователя
    /// с указанным permission claim.
    /// </summary>
    /// <param name="builder">Стандартный builder authorization policy.</param>
    /// <param name="permission">Требуемое разрешение.</param>
    /// <param name="claimType">Тип claim, содержащего permissions.</param>
    /// <returns>Исходный builder authorization policy.</returns>
    public static AuthorizationPolicyBuilder RequirePermission(
        this AuthorizationPolicyBuilder builder,
        string permission,
        string claimType = PermissionAuthorizationDefaults.ClaimType)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        // Используем встроенные требования ASP.NET Core вместо собственного
        // PermissionRequirement/PermissionHandler.
        return builder
            .RequireAuthenticatedUser()
            .RequireClaim(claimType, permission);
    }
}
