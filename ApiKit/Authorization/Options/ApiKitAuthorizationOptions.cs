using System.Text.Json;
using ApiKit.Authorization.Permissions;

namespace ApiKit.Authorization.Options;

/// <summary>
/// Содержит настройки permission-based authorization в ApiKit.
/// </summary>
public sealed class ApiKitAuthorizationOptions
{
    private readonly Dictionary<Type, string> _resourceNames = [];
    private readonly Dictionary<PermissionKey, string> _permissionNames = [];

    /// <summary>
    /// Тип claim, содержащего permissions пользователя.
    /// </summary>
    public string PermissionClaimType { get; set; } = PermissionAuthorizationDefaults.ClaimType;

    internal IReadOnlyDictionary<Type, string> ResourceNames => _resourceNames;

    internal IReadOnlyDictionary<PermissionKey, string> PermissionNames => _permissionNames;

    /// <summary>
    /// Переопределяет имя ресурса, используемое при автоматическом
    /// формировании permissions для указанного типа.
    /// </summary>
    /// <typeparam name="TResource">Тип API-ресурса.</typeparam>
    /// <param name="resourceName">
    /// Имя ресурса. Например, <c>messages</c> приведёт к permissions
    /// <c>messages.read</c>, <c>messages.create</c> и т. д.
    /// </param>
    /// <returns>Текущий объект настроек.</returns>
    public ApiKitAuthorizationOptions MapResource<TResource>(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        _resourceNames[typeof(TResource)] = resourceName.Trim();
        return this;
    }

    /// <summary>
    /// Полностью переопределяет имя permission для стандартной операции.
    /// </summary>
    /// <typeparam name="TResource">Тип API-ресурса.</typeparam>
    /// <param name="operation">Операция над ресурсом.</param>
    /// <param name="permissionName">Итоговое имя permission.</param>
    /// <returns>Текущий объект настроек.</returns>
    public ApiKitAuthorizationOptions MapPermission<TResource>(
        PermissionOperation operation,
        string permissionName)
    {
        return MapPermission<TResource>(operation.ToString(), permissionName);
    }

    /// <summary>
    /// Полностью переопределяет имя permission для произвольной операции.
    /// </summary>
    /// <typeparam name="TResource">Тип API-ресурса.</typeparam>
    /// <param name="operation">Имя операции.</param>
    /// <param name="permissionName">Итоговое имя permission.</param>
    /// <returns>Текущий объект настроек.</returns>
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
