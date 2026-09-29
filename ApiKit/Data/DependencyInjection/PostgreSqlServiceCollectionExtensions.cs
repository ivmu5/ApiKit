using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace ApiKit.Data;

/// <summary>
/// Содержит методы регистрации EF Core <see cref="DbContext"/> для PostgreSQL через Npgsql.
/// </summary>
public static class PostgreSqlServiceCollectionExtensions
{
    /// <summary>
    /// Имя строки подключения, используемое по умолчанию.
    /// </summary>
    public const string DefaultConnectionStringName = "DefaultConnection";

    /// <summary>
    /// Регистрирует <typeparamref name="TContext"/> через стандартный EF Core
    /// <c>AddDbContext</c> и настраивает провайдер PostgreSQL через Npgsql.
    /// </summary>
    /// <typeparam name="TContext">Тип регистрируемого контекста EF Core.</typeparam>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <param name="connectionStringName">
    /// Имя строки подключения в секции <c>ConnectionStrings</c>.
    /// </param>
    /// <param name="configureDbContext">
    /// Дополнительная настройка стандартного <see cref="DbContextOptionsBuilder"/>.
    /// Делегат вызывается после <c>UseNpgsql</c> и может изменять или дополнять настройки EF Core.
    /// </param>
    /// <param name="configureNpgsql">
    /// Дополнительная настройка стандартного <see cref="NpgsqlDbContextOptionsBuilder"/>.
    /// </param>
    /// <param name="contextLifetime">Время жизни экземпляра <typeparamref name="TContext"/> в DI.</param>
    /// <param name="optionsLifetime">Время жизни <see cref="DbContextOptions"/> в DI.</param>
    /// <returns>Исходная коллекция сервисов.</returns>
    /// <remarks>
    /// Метод не вводит собственный DbContext, repository или unit of work.
    /// После регистрации приложение продолжает работать напрямую со стандартными API EF Core и Npgsql.
    /// Если требуется полностью нестандартная регистрация контекста, разработчик может использовать
    /// штатный <c>AddDbContext</c>, <c>AddDbContextPool</c> или <c>AddDbContextFactory</c> напрямую.
    /// </remarks>
    public static IServiceCollection AddApiKitPostgreSqlDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = DefaultConnectionStringName,
        Action<IServiceProvider, DbContextOptionsBuilder>? configureDbContext = null,
        Action<IServiceProvider, NpgsqlDbContextOptionsBuilder>? configureNpgsql = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Строка подключения '{connectionStringName}' не найдена в секции ConnectionStrings.");
        }

        return services.AddApiKitPostgreSqlDbContext<TContext>(
            connectionString,
            configureDbContext,
            configureNpgsql,
            contextLifetime,
            optionsLifetime);
    }

    /// <summary>
    /// Регистрирует <typeparamref name="TContext"/> через стандартный EF Core
    /// <c>AddDbContext</c>, используя переданную строку подключения PostgreSQL.
    /// </summary>
    /// <typeparam name="TContext">Тип регистрируемого контекста EF Core.</typeparam>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="connectionString">Строка подключения PostgreSQL.</param>
    /// <param name="configureDbContext">
    /// Дополнительная настройка стандартного <see cref="DbContextOptionsBuilder"/>.
    /// Делегат вызывается после <c>UseNpgsql</c> и может изменять или дополнять настройки EF Core.
    /// </param>
    /// <param name="configureNpgsql">
    /// Дополнительная настройка стандартного <see cref="NpgsqlDbContextOptionsBuilder"/>.
    /// </param>
    /// <param name="contextLifetime">Время жизни экземпляра <typeparamref name="TContext"/> в DI.</param>
    /// <param name="optionsLifetime">Время жизни <see cref="DbContextOptions"/> в DI.</param>
    /// <returns>Исходная коллекция сервисов.</returns>
    public static IServiceCollection AddApiKitPostgreSqlDbContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        Action<IServiceProvider, DbContextOptionsBuilder>? configureDbContext = null,
        Action<IServiceProvider, NpgsqlDbContextOptionsBuilder>? configureNpgsql = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<TContext>(
            (serviceProvider, options) =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions => configureNpgsql?.Invoke(serviceProvider, npgsqlOptions));

                // Пользовательская настройка вызывается последней, чтобы разработчик
                // мог изменить любую настройку по умолчанию, применённую ApiKit.
                configureDbContext?.Invoke(serviceProvider, options);
            },
            contextLifetime,
            optionsLifetime);

        return services;
    }
}
