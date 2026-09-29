using ApiKit.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace ApiKit.Authorization.Attributes;

/// <summary>
/// Требует явно указанное permission для доступа к endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Создаёт требование для указанного permission.
    /// </summary>
    /// <param name="permission">Требуемое permission.</param>
    public RequirePermissionAttribute(string permission)
    {
        Policy = PermissionPolicyName.ForPermission(permission);
    }
}

/// <summary>
/// Требует permission, автоматически сформированное
/// по типу API-ресурса и операции.
/// </summary>
/// <typeparam name="TResource">Тип API-ресурса.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute<TResource> : AuthorizeAttribute
{
    /// <summary>
    /// Создаёт требование для стандартной операции над ресурсом.
    /// </summary>
    /// <param name="operation">Операция над ресурсом.</param>
    public RequirePermissionAttribute(PermissionOperation operation)
    {
        Policy = PermissionPolicyName.ForResource(typeof(TResource), operation);
    }

    /// <summary>
    /// Создаёт требование для произвольной операции над ресурсом.
    /// </summary>
    /// <param name="operation">Имя операции.</param>
    public RequirePermissionAttribute(string operation)
    {
        Policy = PermissionPolicyName.ForResource(typeof(TResource), operation);
    }
}
