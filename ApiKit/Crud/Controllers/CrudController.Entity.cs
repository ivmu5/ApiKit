using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using ApiKit.Crud.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ApiKit.Crud.Controllers;

/// <summary>
/// Сокращённый CRUD-контроллер, использующий EF Core entity как read/create/update API-модель.
/// </summary>
/// <typeparam name="TDbContext">Тип контекста EF Core.</typeparam>
/// <typeparam name="TEntity">Тип EF Core entity и API-модели.</typeparam>
/// <typeparam name="TKey">Тип первичного ключа.</typeparam>
/// <remarks>
/// Вариант предназначен прежде всего для внутренних или простых API. Для внешнего API
/// отдельные DTO позволяют точнее контролировать поля, доступные клиенту.
/// </remarks>
public abstract class CrudController<TDbContext, TEntity, TKey>
    : CrudController<TDbContext, TEntity, TKey, TEntity, TEntity, TEntity>
    where TDbContext : DbContext
    where TEntity : class
{
    /// <summary>
    /// Создаёт CRUD-контроллер, работающий непосредственно с entity.
    /// </summary>
    /// <param name="dbContext">Контекст EF Core.</param>
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
