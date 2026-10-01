using ApiKit.Crud.Internal;
using ApiKit.Crud.Models;
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
    /// Creates read query.
    /// </summary>
    /// <returns>The created value.</returns>
    protected virtual IQueryable<TEntity> CreateReadQuery() =>
        Entities.AsNoTracking();

    /// <summary>
    /// Creates list query.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    /// <returns>The created value.</returns>
    protected virtual IQueryable<TEntity> CreateListQuery(CrudPageOptions options) =>
        CreateReadQuery();

    /// <summary>
    /// Applies list ordering.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="options">The configuration options.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual IQueryable<TEntity> ApplyListOrdering(
        IQueryable<TEntity> query,
        CrudPageOptions options) =>
        EfPrimaryKeyExpression.OrderBy(
            query,
            EfEntityMetadata.GetSinglePrimaryKeyProperty<TEntity>(DbContext));

    /// <summary>
    /// Applies pagination.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The maximum number of items per page.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual IQueryable<TEntity> ApplyPagination(
        IQueryable<TEntity> query,
        int page,
        int pageSize)
    {
        var offset = checked((page - 1) * pageSize);
        return query.Skip(offset).Take(pageSize);
    }

    /// <summary>
    /// Resolves page size.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual int ResolvePageSize(CrudPageOptions options) =>
        options.PageSize ?? DefaultPageSize;

    /// <summary>
    /// Finds read entity async.
    /// </summary>
    /// <param name="id">The primary key of the resource.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual Task<TEntity?> FindReadEntityAsync(
        TKey id,
        CancellationToken cancellationToken)
    {
        var predicate = EfPrimaryKeyExpression.CreatePredicate<TEntity, TKey>(
            EfEntityMetadata.GetSinglePrimaryKeyProperty<TEntity>(DbContext),
            id);

        return CreateReadQuery().SingleOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Finds entity async.
    /// </summary>
    /// <param name="id">The primary key of the resource.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual ValueTask<TEntity?> FindEntityAsync(
        TKey id,
        CancellationToken cancellationToken) =>
        Entities.FindAsync([id], cancellationToken);

}
