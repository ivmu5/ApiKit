using System.Text.Json;
using ApiKit.Authorization.Options;
using Microsoft.Extensions.Options;

namespace ApiKit.Authorization.Permissions;

internal sealed class PermissionNameResolver(
    IOptions<ApiKitAuthorizationOptions> options) : IPermissionNameResolver
{
    private readonly ApiKitAuthorizationOptions _options = options.Value;

    public string GetPermission<TResource>(PermissionOperation operation) =>
        GetPermission(typeof(TResource), operation.ToString());

    public string GetPermission<TResource>(string operation) =>
        GetPermission(typeof(TResource), operation);

    public string GetPermission(Type resourceType, string operation)
    {
        ArgumentNullException.ThrowIfNull(resourceType);

        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        var normalizedOperation = NormalizeSegment(operation);
        var key = new ApiKitAuthorizationOptions.PermissionKey(
            resourceType,
            normalizedOperation);

        if (_options.PermissionNames.TryGetValue(key, out var permissionName))
        {
            return permissionName;
        }

        var resourceName = _options.ResourceNames.TryGetValue(resourceType, out var mappedName)
            ? mappedName
            : NormalizeSegment(resourceType.Name);

        return $"{resourceName}.{normalizedOperation}";
    }

    private static string NormalizeSegment(string value) =>
        JsonNamingPolicy.KebabCaseLower.ConvertName(value.Trim());
}
