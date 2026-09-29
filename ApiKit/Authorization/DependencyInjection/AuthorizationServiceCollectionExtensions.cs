using ApiKit.Authorization.Options;
using ApiKit.Authorization.Permissions;
using ApiKit.Authorization.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiKit.Authorization;

/// <summary>
/// Содержит методы регистрации permission-based авторизации ApiKit.
/// </summary>
public static class AuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет стандартную ASP.NET Core authorization-инфраструктуру
    /// и поддержку динамических permission policies.
    /// </summary>
    /// <remarks>
    /// ApiKit создаёт permission policies через стандартный
    /// <see cref="IAuthorizationPolicyProvider"/> только при их запросе ASP.NET Core.
    /// Обычные policies приложения обслуживаются стандартным поставщиком ASP.NET Core.
    /// </remarks>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configure">Необязательная настройка соглашений permissions.</param>
    /// <returns>Исходная коллекция сервисов.</returns>
    public static IServiceCollection AddApiKitAuthorization(
        this IServiceCollection services,
        Action<ApiKitAuthorizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorization();

        var optionsBuilder = services.AddOptions<ApiKitAuthorizationOptions>();

        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        optionsBuilder
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.PermissionClaimType),
                $"{nameof(ApiKitAuthorizationOptions.PermissionClaimType)} не может быть пустым.")
            .ValidateOnStart();

        services.TryAddSingleton<IPermissionNameResolver, PermissionNameResolver>();

        // ASP.NET Core использует один IAuthorizationPolicyProvider. Поставщик ApiKit
        // обрабатывает только собственные политики разрешений и делегирует остальные
        // стандартному DefaultAuthorizationPolicyProvider.
        services.Replace(
            ServiceDescriptor.Singleton<
                IAuthorizationPolicyProvider,
                PermissionAuthorizationPolicyProvider>());

        return services;
    }
}
