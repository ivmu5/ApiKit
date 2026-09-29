using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ApiKit.Crud.Controllers;

public abstract partial class CrudController<
    TDbContext,
    TEntity,
    TKey,
    TReadModel,
    TCreateModel,
    TUpdateModel>
    where TDbContext : DbContext
    where TEntity : class
    where TCreateModel : class
    where TUpdateModel : class
{
    /// <summary>
    /// Возвращает LINQ projection для чтения DTO непосредственно в SQL.
    /// </summary>
    /// <returns>Projection или <see langword="null"/>.</returns>
    /// <remarks>
    /// Если projection не задан, query материализуется в entity, после чего
    /// вызывается <see cref="MapReadModelAsync"/>.
    /// </remarks>
    protected virtual Expression<Func<TEntity, TReadModel>>? GetReadProjection() => null;

    /// <summary>
    /// Материализует collection query в публичные read-модели.
    /// </summary>
    /// <param name="query">Запрос entity.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Read-модели.</returns>
    protected virtual async Task<IReadOnlyList<TReadModel>> MaterializeReadModelsAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken)
    {
        var projection = GetReadProjection();

        if (projection is not null)
        {
            return await query.Select(projection).ToListAsync(cancellationToken);
        }

        var entities = await query.ToListAsync(cancellationToken);
        var result = new List<TReadModel>(entities.Count);

        foreach (var entity in entities)
        {
            result.Add(await MapReadModelAsync(entity, cancellationToken));
        }

        return result;
    }

    /// <summary>
    /// Преобразует entity в публичную read-модель.
    /// </summary>
    /// <param name="entity">Исходная entity.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Read-модель.</returns>
    protected virtual ValueTask<TReadModel> MapReadModelAsync(
        TEntity entity,
        CancellationToken cancellationToken)
    {
        var projection = GetReadProjection();

        if (projection is not null)
        {
            return ValueTask.FromResult(projection.Compile().Invoke(entity));
        }

        throw new NotSupportedException(
            $"Контроллер {GetType().Name} должен переопределить {nameof(MapReadModelAsync)} " +
            $"или {nameof(GetReadProjection)}.");
    }

    /// <summary>
    /// Создаёт EF Core entity из POST-модели.
    /// </summary>
    /// <param name="model">Модель создания.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Созданная entity.</returns>
    protected virtual ValueTask<TEntity> CreateEntityAsync(
        TCreateModel model,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            $"Контроллер {GetType().Name} должен переопределить {nameof(CreateEntityAsync)} " +
            $"или action {nameof(Create)}.");

    /// <summary>
    /// Применяет PUT/PATCH update-модель к отслеживаемой entity.
    /// </summary>
    /// <param name="entity">Изменяемая entity.</param>
    /// <param name="model">Модель обновления.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Ожидаемая операция.</returns>
    protected virtual ValueTask ApplyUpdateModelAsync(
        TEntity entity,
        TUpdateModel model,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            $"Контроллер {GetType().Name} должен переопределить {nameof(ApplyUpdateModelAsync)} " +
            $"или соответствующий update action.");

    /// <summary>
    /// Создаёт update-модель из текущего состояния entity перед JSON Patch.
    /// </summary>
    /// <param name="entity">Текущая entity.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Модель обновления.</returns>
    protected virtual ValueTask<TUpdateModel> CreateUpdateModelAsync(
        TEntity entity,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            $"Для стандартного PATCH контроллер {GetType().Name} должен переопределить " +
            $"{nameof(CreateUpdateModelAsync)} или action {nameof(Patch)}.");
}
