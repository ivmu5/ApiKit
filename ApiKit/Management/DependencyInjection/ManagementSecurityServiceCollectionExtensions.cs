using ApiKit.Management.Abstractions;
using ApiKit.Management.Internal;
using ApiKit.Management.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiKit.Management;

/// <summary>
/// Defines extension methods for management security service collection extensions.
/// </summary>
public static class ManagementSecurityServiceCollectionExtensions
{
    /// <summary>
    /// Registers API kit management security.
    /// </summary>
    /// <param name="services">The dependency injection service collection.</param>
    /// <param name="configure">An optional configuration callback.</param>
    /// <returns>The builder or service collection for further configuration.</returns>
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
