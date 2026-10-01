using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace ApiKit.Data;

/// <summary>
/// Defines extension methods for postgre SQL service collection extensions.
/// </summary>
public static class PostgreSqlServiceCollectionExtensions
{
    /// <summary>
    /// Gets or sets default connection string name.
    /// </summary>
    public const string DefaultConnectionStringName = "DefaultConnection";

    /// <summary>
    /// Registers API kit postgre SQL db context.
    /// </summary>
    /// <typeparam name="TContext">The t context type.</typeparam>
    /// <param name="services">The dependency injection service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="connectionStringName">The connection string name value.</param>
    /// <param name="configureDbContext">The configure db context value.</param>
    /// <param name="configureNpgsql">The configure npgsql value.</param>
    /// <param name="contextLifetime">The context lifetime value.</param>
    /// <param name="optionsLifetime">The options lifetime value.</param>
    /// <returns>The builder or service collection for further configuration.</returns>
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
    /// Registers API kit postgre SQL db context.
    /// </summary>
    /// <typeparam name="TContext">The t context type.</typeparam>
    /// <param name="services">The dependency injection service collection.</param>
    /// <param name="connectionString">The connection string value.</param>
    /// <param name="configureDbContext">The configure db context value.</param>
    /// <param name="configureNpgsql">The configure npgsql value.</param>
    /// <param name="contextLifetime">The context lifetime value.</param>
    /// <param name="optionsLifetime">The options lifetime value.</param>
    /// <returns>The builder or service collection for further configuration.</returns>
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

                configureDbContext?.Invoke(serviceProvider, options);
            },
            contextLifetime,
            optionsLifetime);

        return services;
    }
}
