using ApiKit.Management.Abstractions;
using ApiKit.Management.Builders;
using ApiKit.Management.CryptoKit.Internal;
using ApiKit.Management.CryptoKit.Options;
using ApiKit.Management.CryptoKit.Provisioning;
using ApiKit.Management.CryptoKit.Security;
using CryptoKit.Rsa;
using CryptoKit.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiKit.Management.CryptoKit.DependencyInjection;

/// <summary>
/// Registers the CryptoKit adapter, credential rotation, trusted public keys, and provisioning services.
/// </summary>
public static class CryptoKitManagementServiceCollectionExtensions
{
    /// <summary>
    /// Registers API kit management crypto kit.
    /// </summary>
    /// <param name="services">The dependency injection service collection.</param>
    /// <param name="configure">An optional configuration callback.</param>
    /// <returns>The builder or service collection for further configuration.</returns>
    public static IServiceCollection AddApiKitManagementCryptoKit(
        this IServiceCollection services,
        Action<CryptoKitManagementSecurityOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var configuredOptions = new CryptoKitManagementSecurityOptions();
        configure(configuredOptions);

        var snapshot = new CryptoKitManagementOptionsSnapshot(configuredOptions);

        services.AddSingleton(snapshot);
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);

        services.AddSingleton<ICryptoKitManagementTrustStore, CryptoKitManagementTrustStore>();
        services.AddSingleton<CryptoKitManagementCredentialRegistry>();
        services.AddSingleton<ICryptoKitManagementCredentialRotationManager>(
            static provider => provider.GetRequiredService<CryptoKitManagementCredentialRegistry>());
        services.AddSingleton<CryptoKitManagementCredentialProvider>();
        services.AddSingleton<IManagementCredentialProvider>(
            static provider => provider.GetRequiredService<CryptoKitManagementCredentialProvider>());
        services.AddSingleton<ICryptoKitManagementPublicCredentialProvider>(
            static provider => provider.GetRequiredService<CryptoKitManagementCredentialProvider>());
        services.AddSingleton<IManagementPeerVerifier, CryptoKitManagementPeerVerifier>();

        return services;
    }

    /// <summary>
    /// Registers API kit management crypto kit provisioning.
    /// </summary>
    public static IServiceCollection AddApiKitManagementCryptoKitProvisioning(
        this IServiceCollection services,
        Action<CryptoKitManagedServiceProvisioningOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var configured = new CryptoKitManagedServiceProvisioningOptions();
        configure?.Invoke(configured);

        var snapshot = new CryptoKitManagedServiceProvisioningOptionsSnapshot(configured);
        services.TryAddSingleton(snapshot);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IManagedServiceCredentialProvisioner, CryptoKitManagedServiceCredentialProvisioner>());

        return services;
    }

    /// <summary>
    /// Registers API kit provisioned management crypto kit.
    /// </summary>
    /// <param name="services">The dependency injection service collection.</param>
    /// <param name="configure">An optional configuration callback.</param>
    /// <param name="configureRsa">The configure RSA value.</param>
    /// <param name="applicationBaseDirectory">The application base directory value.</param>
    /// <param name="cryptoKitDirectoryName">The crypto kit directory name value.</param>
    public static IServiceCollection AddApiKitProvisionedManagementCryptoKit(
        this IServiceCollection services,
        Action<CryptoKitManagementSecurityOptions>? configure = null,
        Action<RsaKeyOptions>? configureRsa = null,
        string? applicationBaseDirectory = null,
        string cryptoKitDirectoryName = "cryptokit")
    {
        ArgumentNullException.ThrowIfNull(services);

        var storageDirectory = CryptoKitManagementProvisioningPaths
            .GetStorageDirectoryForInstalledApplication(
                applicationBaseDirectory,
                cryptoKitDirectoryName);

        services.AddCryptoKitFileStorage(options =>
            options.DirectoryPath = storageDirectory);
        services.AddCryptoKitRsa(configureRsa);

        return services.AddApiKitManagementCryptoKit(
            configure ?? (static _ => { }));
    }

    /// <summary>
    /// Registers crypto kit security.
    /// </summary>
    /// <param name="builder">The service or endpoint builder.</param>
    /// <param name="configure">An optional configuration callback.</param>
    /// <returns>The builder or service collection for further configuration.</returns>
    public static ApiKitManagementBuilder AddCryptoKitSecurity(
        this ApiKitManagementBuilder builder,
        Action<CryptoKitManagementSecurityOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddApiKitManagementCryptoKit(builder.Services, configure);
        return builder;
    }

}
