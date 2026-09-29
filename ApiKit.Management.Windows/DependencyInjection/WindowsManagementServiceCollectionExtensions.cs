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
/// Регистрирует Windows Named Pipe transport для management plane ApiKit.
/// </summary>
public static class WindowsManagementServiceCollectionExtensions
{
    /// <summary>
    /// Подключает управляемый микросервис к локальному management host через Named Pipes.
    /// </summary>
    /// <remarks>
    /// Метод добавляет publisher регистрации, management pipe сервиса и transport metadata.
    /// По умолчанию pipe доступен только текущей Windows identity.
    /// Начиная с protocol v5 transport требует взаимную криптографическую аутентификацию:
    /// сервис проверяет <see cref="ManagementPeerKind.ManagementHost"/>, а затем доказывает собственную identity.
    /// Для этого должны быть зарегистрированы <see cref="IManagementCredentialProvider"/> и
    /// <see cref="IManagementPeerVerifier"/>.
    /// </remarks>
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
    /// Добавляет локальный management host: registry, registration pipe, административный pipe
    /// и proxy вызовов management-операций в микросервисы.
    /// </summary>
    /// <remarks>
    /// Registration pipe использует взаимный challenge-response: host сначала доказывает собственную identity,
    /// после чего проверяет identity сервиса. Для исходящих management-вызовов host также доказывает свою identity
    /// сервису и проверяет service credential перед передачей запроса.
    /// </remarks>
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
            .Validate(static options => options.OperationTimeout > TimeSpan.Zero, "OperationTimeout должен быть больше нуля.")
            .ValidateOnStart();

        services.TryAddSingleton<IManagementPeerAuthenticator, WindowsNamedPipePeerAuthenticator>();
        services.TryAddSingleton<WindowsNamedPipeManagedServiceClient>();
        services.TryAddSingleton<IManagedServiceLifecycleController, WindowsManagedServiceLifecycleController>();
        services.TryAddSingleton<IManagedServiceInstaller, WindowsManagedServiceInstaller>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, WindowsNamedPipeManagementHostServer>());

        return services;
    }

    /// <summary>
    /// Добавляет клиент админ-панели, использующий административный Named Pipe management host.
    /// </summary>
    /// <remarks>
    /// Protocol v6 использует взаимный challenge-response: клиент сначала проверяет
    /// <see cref="ManagementPeerKind.ManagementHost"/>, затем доказывает собственную
    /// <see cref="ManagementPeerKind.AdminClient"/> identity. Для этого должны быть
    /// зарегистрированы <see cref="IManagementCredentialProvider"/> и
    /// <see cref="IManagementPeerVerifier"/>.
    /// </remarks>
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
    /// Настраивает текущий .NET host для запуска как Windows Service.
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
