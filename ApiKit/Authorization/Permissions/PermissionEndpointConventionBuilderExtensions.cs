using Microsoft.AspNetCore.Builder;

namespace ApiKit.Authorization.Permissions;

/// <summary>
/// Содержит расширения Minimal API для permission-based authorization.
/// </summary>
public static class PermissionEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Требует permission, автоматически сформированное
    /// по типу ресурса и стандартной операции.
    /// </summary>
    /// <typeparam name="TResource">Тип API-ресурса.</typeparam>
    /// <param name="builder">Builder endpoint.</param>
    /// <param name="operation">Операция над ресурсом.</param>
    /// <returns>Исходный builder endpoint.</returns>
    public static IEndpointConventionBuilder RequirePermission<TResource>(
        this IEndpointConventionBuilder builder,
        PermissionOperation operation)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(
            PermissionPolicyName.ForResource(typeof(TResource), operation));
    }

    /// <summary>
    /// Требует permission, автоматически сформированное
    /// по типу ресурса и произвольной операции.
    /// </summary>
    /// <typeparam name="TResource">Тип API-ресурса.</typeparam>
    /// <param name="builder">Builder endpoint.</param>
    /// <param name="operation">Имя операции.</param>
    /// <returns>Исходный builder endpoint.</returns>
    public static IEndpointConventionBuilder RequirePermission<TResource>(
        this IEndpointConventionBuilder builder,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(
            PermissionPolicyName.ForResource(typeof(TResource), operation));
    }

    /// <summary>
    /// Требует явно указанное permission.
    /// </summary>
    /// <param name="builder">Builder endpoint.</param>
    /// <param name="permission">Требуемое permission.</param>
    /// <returns>Исходный builder endpoint.</returns>
    public static IEndpointConventionBuilder RequirePermission(
        this IEndpointConventionBuilder builder,
        string permission)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(
            PermissionPolicyName.ForPermission(permission));
    }
}
