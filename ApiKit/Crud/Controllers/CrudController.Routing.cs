using Microsoft.AspNetCore.Routing;
using ApiKit.Crud.Internal;
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
    /// Формирует route values для ответа 201 Created.
    /// </summary>
    /// <param name="entity">Созданная entity.</param>
    /// <returns>Значения маршрута к GET by id.</returns>
    protected virtual object GetCreatedRouteValues(TEntity entity) =>
        new RouteValueDictionary
        {
            ["id"] = DbContext.Entry(entity)
                .Property(EfEntityMetadata.GetSinglePrimaryKeyProperty<TEntity>(DbContext).Name)
                .CurrentValue
        };
}
