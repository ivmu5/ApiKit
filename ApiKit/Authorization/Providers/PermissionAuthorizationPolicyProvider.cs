using ApiKit.Authorization.Options;
using ApiKit.Authorization.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ApiKit.Authorization.Providers;

internal sealed class PermissionAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _defaultProvider;
    private readonly IPermissionNameResolver _permissionNameResolver;
    private readonly ApiKitAuthorizationOptions _apiKitOptions;

    public PermissionAuthorizationPolicyProvider(
        IOptions<AuthorizationOptions> authorizationOptions,
        IOptions<ApiKitAuthorizationOptions> apiKitOptions,
        IPermissionNameResolver permissionNameResolver)
    {
        ArgumentNullException.ThrowIfNull(authorizationOptions);
        ArgumentNullException.ThrowIfNull(apiKitOptions);
        ArgumentNullException.ThrowIfNull(permissionNameResolver);

        _defaultProvider = new DefaultAuthorizationPolicyProvider(authorizationOptions);
        _apiKitOptions = apiKitOptions.Value;
        _permissionNameResolver = permissionNameResolver;
    }

    public bool AllowsCachingPolicies => true;

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        if (!PermissionPolicyName.TryParse(policyName, out var descriptor))
        {
            return _defaultProvider.GetPolicyAsync(policyName);
        }

        var permission = descriptor.Permission
            ?? _permissionNameResolver.GetPermission(
                descriptor.ResourceType
                    ?? throw new InvalidOperationException(
                        "В permission policy отсутствует тип ресурса."),
                descriptor.Operation
                    ?? throw new InvalidOperationException(
                        "В permission policy отсутствует операция."));

        var policy = new AuthorizationPolicyBuilder()
            .RequirePermission(permission, _apiKitOptions.PermissionClaimType)
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _defaultProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _defaultProvider.GetFallbackPolicyAsync();
}
