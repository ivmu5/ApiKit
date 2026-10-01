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
/// Registers and configures JWT generation and bearer authentication for ApiKit.
/// </summary>
public static class JwtServiceCollectionExtensions
{
    /// <summary>
    /// Registers API kit JWT token generation.
    /// </summary>
    /// <param name="services">The dependency injection service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The builder or service collection for further configuration.</returns>
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
    /// Registers API kit JWT bearer authentication.
    /// </summary>
    /// <param name="services">The dependency injection service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="authenticationScheme">The authentication scheme name.</param>
    /// <param name="setAsDefaultScheme">Whether to register this as the default authentication scheme.</param>
    /// <param name="configure">An optional configuration callback.</param>
    /// <returns>The builder or service collection for further configuration.</returns>
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
