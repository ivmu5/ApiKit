using ApiKit.Authentication.Abstractions;
using ApiKit.Authentication.Internal;
using ApiKit.Authentication.Options;
using ApiKit.Authentication.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApiKit.Authentication;

/// <summary>
/// Содержит методы регистрации основной JWT-инфраструктуры ApiKit.
/// </summary>
public static class JwtServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет сервис создания JWT-токенов и загружает настройки
    /// из секции <c>Jwt:Generation</c>.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <returns>Исходная коллекция сервисов.</returns>
    public static IServiceCollection AddApiKitJwtTokenGeneration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton<TimeProvider>(TimeProvider.System);

        services
            .AddOptions<JwtTokenGenerationOptions>()
            .Bind(configuration.GetSection(JwtTokenGenerationOptions.SectionName))
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.Issuer),
                "Издатель JWT-токена не может быть пустым.")
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.Audience),
                "Аудитория JWT-токена не может быть пустой.")
            .Validate(
                static options => options.DefaultLifetime > TimeSpan.Zero,
                "JWT DefaultLifetime должен быть больше нуля.")
            .ValidateOnStart();

        services.TryAddTransient<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }

    /// <summary>
    /// Добавляет JWT Bearer-аутентификацию и загружает настройки проверки
    /// из секции <c>Jwt:Validation</c>.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <param name="authenticationScheme">Имя схемы аутентификации.</param>
    /// <param name="setAsDefaultScheme">
    /// Если <see langword="true"/>, схема становится схемой аутентификации по умолчанию.
    /// </param>
    /// <param name="configure">
    /// Необязательная дополнительная настройка стандартного <see cref="JwtBearerOptions"/>.
    /// Вызывается после применения настроек ApiKit и может переопределить их.
    /// </param>
    /// <returns>Исходная коллекция сервисов.</returns>
    public static IServiceCollection AddApiKitJwtBearerAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string authenticationScheme = JwtBearerDefaults.AuthenticationScheme,
        bool setAsDefaultScheme = true,
        Action<JwtBearerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationScheme);

        services
            .AddOptions<JwtBearerValidationOptions>()
            .Bind(configuration.GetSection(JwtBearerValidationOptions.SectionName))
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.ValidIssuer),
                "Допустимый издатель JWT-токена не может быть пустым.")
            .Validate(
                static options => options.ValidAudiences is { Length: > 0 }
                    && options.ValidAudiences.All(static audience => !string.IsNullOrWhiteSpace(audience)),
                "Необходимо указать хотя бы одну непустую аудиторию JWT-токена.")
            .Validate(
                static options => options.ClockSkew >= TimeSpan.Zero,
                "JWT ClockSkew не может быть отрицательным.")
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.RoleClaimType),
                "Тип claim для ролей не может быть пустым.")
            .ValidateOnStart();

        var authenticationBuilder = setAsDefaultScheme
            ? services.AddAuthentication(authenticationScheme)
            : services.AddAuthentication();

        authenticationBuilder.AddJwtBearer(authenticationScheme, static _ => { });

        services
            .AddOptions<JwtBearerOptions>(authenticationScheme)
            .Configure<
                IOptions<JwtBearerValidationOptions>,
                IJwtValidationKeysProvider>(
                (bearerOptions, validationOptions, keyProvider) =>
                {
                    // По умолчанию сохраняем исходные имена утверждений JWT. При необходимости
                    // приложение может изменить это поведение через переданный делегат настройки.
                    bearerOptions.MapInboundClaims = false;
                    bearerOptions.TokenValidationParameters =
                        JwtTokenValidationParametersFactory.Create(
                            validationOptions.Value,
                            keyProvider);

                    configure?.Invoke(bearerOptions);
                });

        return services;
    }
}
