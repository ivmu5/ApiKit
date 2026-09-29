namespace ApiKit.Authorization.Permissions;

/// <summary>
/// Содержит соглашения ApiKit для permission-based authorization.
/// </summary>
public static class PermissionAuthorizationDefaults
{
    /// <summary>
    /// Тип claim, содержащего отдельное permission пользователя.
    /// </summary>
    public const string ClaimType = "permission";
}
