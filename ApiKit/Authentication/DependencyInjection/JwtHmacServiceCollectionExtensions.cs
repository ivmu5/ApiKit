using System.Text;
using ApiKit.Authentication.Abstractions;
using ApiKit.Authentication.Options;
using ApiKit.Authentication.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiKit.Authentication;

/// <summary>
/// Содержит методы регистрации HMAC-SHA256 ключей для JWT.
/// </summary>
public static class JwtHmacServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет HMAC-SHA256 провайдер ключей JWT и загружает секретный ключ
    /// из секции <c>Jwt:Hmac</c>.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <returns>Исходная коллекция сервисов.</returns>
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
