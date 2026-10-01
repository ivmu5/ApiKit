using System.Text;
using ApiKit.Authentication.Abstractions;
using ApiKit.Authentication.Options;
using ApiKit.Authentication.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiKit.Authentication;

/// <summary>
/// Defines extension methods for JWT HMAC service collection extensions.
/// </summary>
public static class JwtHmacServiceCollectionExtensions
{
    /// <summary>
    /// Registers API kit JWT HMAC key provider.
    /// </summary>
    /// <param name="services">The dependency injection service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The builder or service collection for further configuration.</returns>
    public static IServiceCollection AddApiKitJwtHmacKeyProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<JwtHmacOptions>()
            .Bind(configuration.GetSection(JwtHmacOptions.SectionName))
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.Key),
                "HMAC-ключ JWT не может быть пустым.")
            .Validate(
                static options => string.IsNullOrWhiteSpace(options.Key)
                    || Encoding.UTF8.GetByteCount(options.Key) >= 32,
                "HMAC-ключ JWT должен иметь длину не менее 32 байт.")
            .ValidateOnStart();

        services.TryAddSingleton<HmacJwtKeyProvider>();
        services.TryAddSingleton<IJwtSigningCredentialsProvider>(
            static provider => provider.GetRequiredService<HmacJwtKeyProvider>());
        services.TryAddSingleton<IJwtValidationKeysProvider>(
            static provider => provider.GetRequiredService<HmacJwtKeyProvider>());

        return services;
    }
}
