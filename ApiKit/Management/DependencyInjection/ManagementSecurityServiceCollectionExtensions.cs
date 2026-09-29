using ApiKit.Management.Abstractions;
using ApiKit.Management.Internal;
using ApiKit.Management.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiKit.Management;

/// <summary>
/// Содержит регистрацию transport-independent компонентов management security.
/// </summary>
public static class ManagementSecurityServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет одноразовый in-memory challenge provider для challenge-response
    /// аутентификации management peer.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configure">Необязательная настройка challenge.</param>
    /// <returns>Исходная коллекция сервисов.</returns>
    public static IServiceCollection AddApiKitManagementSecurity(
        this IServiceCollection services,
        Action<ManagementSecurityOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<ManagementSecurityOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        optionsBuilder
            .Validate(
                static options => options.ChallengeLifetime > TimeSpan.Zero,
                "ChallengeLifetime должен быть больше нуля.")
            .Validate(
                static options => options.NonceSizeBytes is >= 16 and <= 128,
                "NonceSizeBytes должен находиться в диапазоне от 16 до 128 байт.")
            .Validate(
                static options => options.MaximumPendingChallenges is >= 1 and <= 65_536,
                "MaximumPendingChallenges должен находиться в диапазоне от 1 до 65536.")
            .ValidateOnStart();

        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddSingleton<IManagementChallengeProvider, InMemoryManagementChallengeProvider>();

        return services;
    }
}
