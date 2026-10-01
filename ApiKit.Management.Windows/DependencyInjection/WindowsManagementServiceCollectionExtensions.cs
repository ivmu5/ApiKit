using ApiKit.Management.Abstractions;
using ApiKit.Management.Builders;
using ApiKit.Management.Security;
using ApiKit.Management.Windows.Internal;
using ApiKit.Management.Windows.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ApiKit.Management.Windows;

/// <summary>
/// Registers the Windows management transport, management host, and administrative client.
/// </summary>
public static class WindowsManagementServiceCollectionExtensions
{
    /// <summary>
    /// Registers windows named pipe transport.
    /// </summary>
    public static ApiKitManagementBuilder AddWindowsNamedPipeTransport(
        this ApiKitManagementBuilder builder,
        Action<WindowsManagementServiceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var optionsBuilder = builder.Services.AddOptions<WindowsManagementServiceOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        optionsBuilder.PostConfigure(
            static options => WindowsManagedServiceIsolation.ApplyProvisionedManagementPipeAccess(options));

        optionsBuilder
            .Validate(static options => !string.IsNullOrWhiteSpace(options.ManagementHostPeerId), "ManagementHostPeerId не может быть пустым.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.RegistrationPipeName), "RegistrationPipeName не может быть пустым.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.ManagementPipePrefix), "ManagementPipePrefix не может быть пустым.")
            .Validate(static options => options.MaxMessageBytes > 0, "MaxMessageBytes должен быть больше нуля.")
            .Validate(static options => options.ConnectTimeout > TimeSpan.Zero, "ConnectTimeout должен быть больше нуля.")
            .ValidateOnStart();

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(
                ApiKitManagementAuthorizationDefaults.ManagementHostPolicyName,
                static policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(
                        ApiKitManagementAuthorizationDefaults.ClaimType,
                        ApiKitManagementAuthorizationDefaults.ManagementHostClaimValue));

        builder.Services.TryAddSingleton<IManagementPeerAuthenticator, WindowsNamedPipePeerAuthenticator>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IManagementDescriptorContributor, WindowsNamedPipeTransportDescriptorContributor>());
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IManagementServicePublisher, WindowsNamedPipeManagementServicePublisher>());
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, WindowsNamedPipeServiceServer>());

        return builder;
    }

    /// <summary>
    /// Registers API kit windows management host.
    /// </summary>
    public static IServiceCollection AddApiKitWindowsManagementHost(
        this IServiceCollection services,
        Action<WindowsManagementHostOptions>? configure = null,
        Action<WindowsServiceManagementOptions>? configureServices = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        ApiKit.Management.ManagementServiceCollectionExtensions.AddApiKitManagementRegistry(services);
        services.AddAuthorizationBuilder()
            .AddPolicy(
                ApiKitManagementAuthorizationDefaults.PolicyName,
                static policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(
                        ApiKitManagementAuthorizationDefaults.ClaimType,
                        ApiKitManagementAuthorizationDefaults.AdministratorClaimValue))
            .AddPolicy(
                ApiKitManagementAuthorizationDefaults.ServiceRegistrationPolicyName,
                static policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(
                        ApiKitManagementAuthorizationDefaults.ClaimType,
                        ApiKitManagementAuthorizationDefaults.ServiceRegistrationClaimValue))
            .AddPolicy(
                ApiKitManagementAuthorizationDefaults.LifecyclePolicyName,
                static policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(
                        ApiKitManagementAuthorizationDefaults.ClaimType,
                        ApiKitManagementAuthorizationDefaults.LifecycleClaimValue))
            .AddPolicy(
                ApiKitManagementAuthorizationDefaults.PackageManagementPolicyName,
                static policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(
                        ApiKitManagementAuthorizationDefaults.ClaimType,
                        ApiKitManagementAuthorizationDefaults.PackageManagementClaimValue));

        var hostOptions = services.AddOptions<WindowsManagementHostOptions>();
        if (configure is not null)
        {
            hostOptions.Configure(configure);
        }

        hostOptions.PostConfigure(static options =>
        {
            if (!options.UseProvisionedServiceSidAcl)
            {
                return;
            }

            options.RegistrationPipeAccess.CurrentUserOnly = false;
            options.AdministrationPipeAccess.CurrentUserOnly = false;

            if (options.AllowBuiltInAdministratorsOnAdministrationPipe)
            {
                options.AdministrationPipeAccess.AllowBuiltInAdministrators();
            }
        });

        hostOptions
            .Validate(static options => !string.IsNullOrWhiteSpace(options.ManagementHostPeerId), "ManagementHostPeerId не может быть пустым.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.RegistrationPipeName), "RegistrationPipeName не может быть пустым.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.AdministrationPipeName), "AdministrationPipeName не может быть пустым.")
            .Validate(static options => options.MaxMessageBytes > 0, "MaxMessageBytes должен быть больше нуля.")
            .Validate(static options => options.ServiceConnectTimeout > TimeSpan.Zero, "ServiceConnectTimeout должен быть больше нуля.")
            .ValidateOnStart();

        var serviceManagementOptions = services.AddOptions<WindowsServiceManagementOptions>();
        if (configureServices is not null)
        {
            serviceManagementOptions.Configure(configureServices);
        }

        serviceManagementOptions
            .Validate(static options => !string.IsNullOrWhiteSpace(options.InstallRoot), "InstallRoot не может быть пустым.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.ServiceAccount), "ServiceAccount не может быть пустым.")
            .Validate(
                static options => !options.ProtectManagedDirectories || options.EnableServiceSidIsolation,
                "ProtectManagedDirectories требует EnableServiceSidIsolation, иначе сервис не получит уникальный SID для ACL.")
            .Validate(static options => options.OperationTimeout > TimeSpan.Zero, "OperationTimeout должен быть больше нуля.")
            .Validate(static options => options.RegistrationTimeout > TimeSpan.Zero, "RegistrationTimeout должен быть больше нуля.")
            .ValidateOnStart();

        services.TryAddSingleton<IManagementPeerAuthenticator, WindowsNamedPipePeerAuthenticator>();
        services.TryAddSingleton<WindowsManagedServiceIsolationRegistry>();
        services.TryAddSingleton<WindowsNamedPipeManagedServiceClient>();
        services.TryAddSingleton<IManagedServiceLifecycleController, WindowsManagedServiceLifecycleController>();
        services.TryAddSingleton<IManagedServiceInstaller, WindowsManagedServiceInstaller>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, WindowsNamedPipeManagementHostServer>());

        return services;
    }

    /// <summary>
    /// Registers API kit windows management client.
    /// </summary>
    public static IServiceCollection AddApiKitWindowsManagementClient(
        this IServiceCollection services,
        Action<WindowsManagementClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddApiKitManagementSecurity();

        var optionsBuilder = services.AddOptions<WindowsManagementClientOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        optionsBuilder
            .Validate(static options => !string.IsNullOrWhiteSpace(options.AdminClientPeerId), "AdminClientPeerId не может быть пустым.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.ManagementHostPeerId), "ManagementHostPeerId не может быть пустым.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.AdministrationPipeName), "AdministrationPipeName не может быть пустым.")
            .Validate(static options => options.ConnectTimeout > TimeSpan.Zero, "ConnectTimeout должен быть больше нуля.")
            .Validate(static options => options.MaxMessageBytes > 0, "MaxMessageBytes должен быть больше нуля.")
            .ValidateOnStart();

        services.TryAddSingleton<WindowsNamedPipeManagementClient>();
        services.TryAddSingleton<IManagementServiceCatalog>(
            static provider => provider.GetRequiredService<WindowsNamedPipeManagementClient>());
        services.TryAddSingleton<IManagementOperationClient>(
            static provider => provider.GetRequiredService<WindowsNamedPipeManagementClient>());
        services.TryAddSingleton<IManagementResourceClient>(
            static provider => provider.GetRequiredService<WindowsNamedPipeManagementClient>());
        services.TryAddSingleton<IManagedServiceLifecycleController>(
            static provider => provider.GetRequiredService<WindowsNamedPipeManagementClient>());
        services.TryAddSingleton<IManagedServiceInstaller>(
            static provider => provider.GetRequiredService<WindowsNamedPipeManagementClient>());

        return services;
    }

    /// <summary>
    /// Registers API kit windows service.
    /// </summary>
    public static IServiceCollection AddApiKitWindowsService(
        this IServiceCollection services,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        services.AddWindowsService(options => options.ServiceName = serviceName);
        return services;
    }
}
