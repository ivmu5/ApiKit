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
    /// Returns read projection.
    /// </summary>
    /// <returns>The requested value.</returns>
    protected virtual Expression<Func<TEntity, TReadModel>>? GetReadProjection() => null;

    /// <summary>
    /// Materializes projected read models asynchronously using EF Core.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The result of the operation.</returns>
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
    /// Configures a mapping for read model async.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The result of the operation.</returns>
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
    /// Creates entity async.
    /// </summary>
    /// <param name="model">The input data model.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The created value.</returns>
    protected virtual ValueTask<TEntity> CreateEntityAsync(
        TCreateModel model,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            $"Контроллер {GetType().Name} должен переопределить {nameof(CreateEntityAsync)} " +
            $"или action {nameof(Create)}.");

    /// <summary>
    /// Applies update model async.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="model">The input data model.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual ValueTask ApplyUpdateModelAsync(
        TEntity entity,
        TUpdateModel model,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            $"Контроллер {GetType().Name} должен переопределить {nameof(ApplyUpdateModelAsync)} " +
            $"или соответствующий update action.");

    /// <summary>
    /// Creates update model async.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The created value.</returns>
    protected virtual ValueTask<TUpdateModel> CreateUpdateModelAsync(
        TEntity entity,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException(
            $"Для стандартного PATCH контроллер {GetType().Name} должен переопределить " +
            $"{nameof(CreateUpdateModelAsync)} или action {nameof(Patch)}.");
}
