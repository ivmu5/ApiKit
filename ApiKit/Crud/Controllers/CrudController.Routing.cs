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
    /// Returns created route values.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <returns>The requested value.</returns>
    protected virtual object GetCreatedRouteValues(TEntity entity) =>
        new RouteValueDictionary
        {
            ["id"] = DbContext.Entry(entity)
                .Property(EfEntityMetadata.GetSinglePrimaryKeyProperty<TEntity>(DbContext).Name)
                .CurrentValue
        };
}
