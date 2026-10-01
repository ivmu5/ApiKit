using ApiKit.Authorization.Options;
using ApiKit.Authorization.Permissions;
using ApiKit.Authorization.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiKit.Authorization;

/// <summary>
/// Defines extension methods for authorization service collection extensions.
/// </summary>
public static class AuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers API kit authorization.
    /// </summary>
    /// <param name="services">The dependency injection service collection.</param>
    /// <param name="configure">An optional configuration callback.</param>
    /// <returns>The builder or service collection for further configuration.</returns>
    public static IServiceCollection AddApiKitAuthorization(
        this IServiceCollection services,
        Action<ApiKitAuthorizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorization();

        var optionsBuilder = services.AddOptions<ApiKitAuthorizationOptions>();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        optionsBuilder
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.PermissionClaimType),
                $"{nameof(ApiKitAuthorizationOptions.PermissionClaimType)} не может быть пустым.")
            .ValidateOnStart();

        services.TryAddSingleton<IPermissionNameResolver, PermissionNameResolver>();

        services.Replace(
            ServiceDescriptor.Singleton<
                IAuthorizationPolicyProvider,
                PermissionAuthorizationPolicyProvider>());

        return services;
    }
}
