using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using ApiKit.Crud.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ApiKit.Crud.Controllers;

/// <summary>
/// Implements generic EF Core CRUD endpoints using separate DTO models.
/// </summary>
/// <typeparam name="TDbContext">The EF Core database context type.</typeparam>
/// <typeparam name="TEntity">The EF Core entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class CrudController<TDbContext, TEntity, TKey>
    : CrudController<TDbContext, TEntity, TKey, TEntity, TEntity, TEntity>
    where TDbContext : DbContext
    where TEntity : class
{
    /// <summary>
    /// Implements generic EF Core CRUD endpoints using separate DTO models.
    /// </summary>
    /// <param name="dbContext">The db context value.</param>
    protected CrudController(TDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc />
    protected override Expression<Func<TEntity, TEntity>>? GetReadProjection() =>
        entity => entity;

    /// <inheritdoc />
    protected override ValueTask<TEntity> MapReadModelAsync(
        TEntity entity,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(entity);

    /// <inheritdoc />
    protected override ValueTask<TEntity> CreateEntityAsync(
        TEntity model,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(model);

    /// <inheritdoc />
    protected override ValueTask<TEntity> CreateUpdateModelAsync(
        TEntity entity,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(entity);

    /// <inheritdoc />
    protected override ValueTask ApplyUpdateModelAsync(
        TEntity entity,
        TEntity model,
        CancellationToken cancellationToken)
    {
        var targetEntry = DbContext.Entry(entity);
        var sourceEntry = DbContext.Entry(model);

        foreach (var property in GetMutableProperties())
        {
            targetEntry.Property(property.Name).CurrentValue =
                sourceEntry.Property(property.Name).CurrentValue;
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override IReadOnlySet<string> GetPatchablePaths()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in GetMutableProperties())
        {
            if (property.PropertyInfo is null)
            {
                continue;
            }

            result.Add(property.Name);

            if (property.PropertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>() is { } jsonName)
            {
                result.Add(jsonName.Name);
            }
        }

        return result;
    }

    private IEnumerable<IProperty> GetMutableProperties()
    {
        var entityType = EfEntityMetadata.GetEntityType<TEntity>(DbContext);
        var primaryKey = EfEntityMetadata.GetPrimaryKey<TEntity>(DbContext);

        var keyNames = primaryKey.Properties
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        return entityType.GetProperties().Where(property =>
            !keyNames.Contains(property.Name)
            && !property.IsShadowProperty()
            && !property.IsConcurrencyToken
            && property.GetAfterSaveBehavior() == PropertySaveBehavior.Save
            && property.PropertyInfo is { } propertyInfo
            && propertyInfo.SetMethod?.IsPublic == true
            && propertyInfo.GetCustomAttribute<JsonIgnoreAttribute>() is null);
    }
}
