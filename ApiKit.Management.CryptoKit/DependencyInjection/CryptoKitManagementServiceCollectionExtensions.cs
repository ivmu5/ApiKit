using ApiKit.Management.Abstractions;
using ApiKit.Management.Builders;
using ApiKit.Management.CryptoKit.Internal;
using ApiKit.Management.CryptoKit.Options;
using ApiKit.Management.CryptoKit.Security;
using CryptoKit.Rsa;
using CryptoKit.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiKit.Management.CryptoKit.DependencyInjection;

/// <summary>
/// Содержит методы расширения для подключения CryptoKit к management security ApiKit.
/// </summary>
public static class CryptoKitManagementServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует RSA-backed management credential provider, peer verifier и trust store поверх CryptoKit.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configure">Настройка локальных management credentials.</param>
    /// <returns>Исходная коллекция сервисов для цепочного вызова.</returns>
    /// <remarks>
    /// <para>
    /// До вызова этого метода приложение должно зарегистрировать <see cref="IKeyStorage"/>
    /// и <see cref="IRsaKeyProvider"/> средствами CryptoKit.
    /// </para>
    /// <para>
    /// Закрытый RSA-ключ используется только локальным credential provider. Trusted peer keys
    /// сохраняются отдельно как public SPKI через <see cref="ICryptoKitManagementTrustStore"/>,
    /// поэтому проверяющая сторона не обязана владеть закрытыми ключами других сервисов.
    /// </para>
    /// </remarks>
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
        services.AddSingleton<CryptoKitManagementCredentialProvider>();
        services.AddSingleton<IManagementCredentialProvider>(
            static provider => provider.GetRequiredService<CryptoKitManagementCredentialProvider>());
        services.AddSingleton<ICryptoKitManagementPublicCredentialProvider>(
            static provider => provider.GetRequiredService<CryptoKitManagementCredentialProvider>());
        services.AddSingleton<IManagementPeerVerifier, CryptoKitManagementPeerVerifier>();

        return services;
    }
    /// <summary>
    /// Подключает CryptoKit-backed management security к уже созданному <see cref="ApiKitManagementBuilder"/>.
    /// </summary>
    /// <param name="builder">Builder management plane.</param>
    /// <param name="configure">Настройка локальных management credentials.</param>
    /// <returns>Исходный builder для продолжения цепочки регистрации.</returns>
    public static ApiKitManagementBuilder AddCryptoKitSecurity(
        this ApiKitManagementBuilder builder,
        Action<CryptoKitManagementSecurityOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddApiKitManagementCryptoKit(builder.Services, configure);
        return builder;
    }

}
