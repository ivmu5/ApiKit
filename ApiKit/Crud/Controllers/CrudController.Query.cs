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
    /// Создаёт базовый запрос чтения без change tracking.
    /// </summary>
    /// <returns>Базовый запрос чтения.</returns>
    protected virtual IQueryable<TEntity> CreateReadQuery() =>
        Entities.AsNoTracking();

    /// <summary>
    /// Формирует collection query до подсчёта и пагинации.
    /// </summary>
    /// <param name="options">Параметры пагинации.</param>
    /// <returns>Запрос коллекции.</returns>
    /// <remarks>
    /// Это основная точка расширения для обычного LINQ: <c>Where</c>, <c>Include</c>
    /// и других предметных ограничений. Если нужен собственный набор query-параметров,
    /// можно переопределить <see cref="GetAll"/> целиком.
    /// </remarks>
    protected virtual IQueryable<TEntity> CreateListQuery(CrudPageOptions options) =>
        CreateReadQuery();

    /// <summary>
    /// Применяет порядок к collection query перед offset-пагинацией.
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <param name="options">Параметры пагинации.</param>
    /// <returns>Упорядоченный запрос.</returns>
    /// <remarks>
    /// По умолчанию используется единственный первичный ключ, чтобы порядок страниц был
    /// детерминированным. Пользовательскую сортировку можно задать обычным LINQ.
    /// </remarks>
    protected virtual IQueryable<TEntity> ApplyListOrdering(
        IQueryable<TEntity> query,
        CrudPageOptions options) =>
        EfPrimaryKeyExpression.OrderBy(
            query,
            EfEntityMetadata.GetSinglePrimaryKeyProperty<TEntity>(DbContext));

    /// <summary>
    /// Применяет стандартную offset-пагинацию через <c>Skip</c>/<c>Take</c>.
    /// </summary>
    /// <param name="query">Исходный запрос.</param>
    /// <param name="page">Номер страницы, начиная с 1.</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <returns>Запрос с применённой пагинацией.</returns>
    protected virtual IQueryable<TEntity> ApplyPagination(
        IQueryable<TEntity> query,
        int page,
        int pageSize)
    {
        var offset = checked((page - 1) * pageSize);
        return query.Skip(offset).Take(pageSize);
    }

    /// <summary>
    /// Возвращает итоговый размер страницы.
    /// </summary>
    /// <param name="options">Параметры пагинации.</param>
    /// <returns>Размер страницы.</returns>
    protected virtual int ResolvePageSize(CrudPageOptions options) =>
        options.PageSize ?? DefaultPageSize;

    /// <summary>
    /// Ищет entity для GET без change tracking.
    /// </summary>
    /// <param name="id">Первичный ключ.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденная entity или <see langword="null"/>.</returns>
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
    /// Ищет отслеживаемую entity для операций изменения.
    /// </summary>
    /// <param name="id">Первичный ключ.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденная entity или <see langword="null"/>.</returns>
    protected virtual ValueTask<TEntity?> FindEntityAsync(
        TKey id,
        CancellationToken cancellationToken) =>
        Entities.FindAsync([id], cancellationToken);

}
