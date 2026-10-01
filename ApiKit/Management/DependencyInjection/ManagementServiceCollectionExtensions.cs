using ApiKit.Management.Abstractions;
using ApiKit.Management.Builders;
using ApiKit.Management.Internal;
using ApiKit.Management.Options;
using ApiKit.Management.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiKit.Management;

/// <summary>
/// Defines extension methods for management service collection extensions.
/// </summary>
public static class ManagementServiceCollectionExtensions
{
    /// <summary>
    /// Registers API kit management.
    /// </summary>
    /// <param name="services">The dependency injection service collection.</param>
    /// <param name="configure">An optional configuration callback.</param>
    /// <returns>The builder or service collection for further configuration.</returns>
    public static ApiKitManagementBuilder AddApiKitManagement(
        this IServiceCollection services,
        Action<ApiKitManagementOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<ApiKitManagementOptions>();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        optionsBuilder
            .Validate(
                static options => options.HeartbeatInterval > TimeSpan.Zero,
                "HeartbeatInterval должен быть больше нуля.")
            .Validate(
                static options => options.LeaseDuration > options.HeartbeatInterval,
                "LeaseDuration должен быть больше HeartbeatInterval.")
            .Validate(
                static options => options.Capabilities.All(
                    capability => !string.IsNullOrWhiteSpace(capability)),
                "Capabilities не должны содержать пустые значения.")
            .Validate(
                static options => options.Metadata.Keys.All(
                    key => !string.IsNullOrWhiteSpace(key)),
                "Ключи Metadata не должны быть пустыми.")
            .ValidateOnStart();

        services.AddApiKitManagementSecurity();

        services.TryAddSingleton<ManagementServiceIdentityProvider>();
        services.TryAddSingleton<IManagementServiceIdentityProvider>(
            static provider => provider.GetRequiredService<ManagementServiceIdentityProvider>());
        services.TryAddSingleton<IManagementServiceDescriptorProvider, DefaultManagementServiceDescriptorProvider>();
        services.TryAddSingleton<ManagementModelDescriptorFactory>();
        services.TryAddSingleton<CrudManagementResourceRegistry>();
        services.TryAddSingleton<IManagementResourceDispatcher, ManagementResourceDispatcher>();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IManagementDescriptorContributor, AspNetCoreServerAddressContributor>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IManagementDescriptorContributor, AspNetCoreEndpointDescriptorContributor>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IManagementDescriptorContributor, CrudResourceDescriptorContributor>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IManagementDescriptorContributor, ManagementOperationDescriptorContributor>());

        services.TryAddSingleton<IManagementOperationDispatcher, ManagementOperationDispatcher>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, ManagementRegistrationHostedService>());

        services.AddAuthorizationBuilder()
            .AddPolicy(
                ApiKitManagementAuthorizationDefaults.PolicyName,
                static policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(
                        ApiKitManagementAuthorizationDefaults.ClaimType,
                        ApiKitManagementAuthorizationDefaults.AdministratorClaimValue));

        return new ApiKitManagementBuilder(services);
    }

    /// <summary>
    /// Registers API kit management registry.
    /// </summary>
    public static IServiceCollection AddApiKitManagementRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddApiKitManagementSecurity();
        services.TryAddSingleton<IManagementServiceRegistry, InMemoryManagementServiceRegistry>();
        services.TryAddSingleton<IManagementServiceCatalog>(
            static provider => provider.GetRequiredService<IManagementServiceRegistry>());
        return services;
    }
}
