using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiKit.Crud.Controllers;

/// <summary>
/// Implements generic EF Core CRUD endpoints using separate DTO models.
/// </summary>
/// <typeparam name="TDbContext">The EF Core database context type.</typeparam>
/// <typeparam name="TEntity">The EF Core entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
/// <typeparam name="TReadModel">The read model type.</typeparam>
/// <typeparam name="TCreateModel">The create model type.</typeparam>
/// <typeparam name="TUpdateModel">The update model type.</typeparam>
[ApiController]
[Route("api/[controller]")]
public abstract partial class CrudController<
    TDbContext,
    TEntity,
    TKey,
    TReadModel,
    TCreateModel,
    TUpdateModel> : ControllerBase
    where TDbContext : DbContext
    where TEntity : class
    where TCreateModel : class
    where TUpdateModel : class
{
    /// <summary>
    /// Implements generic EF Core CRUD endpoints using separate DTO models.
    /// </summary>
    /// <param name="dbContext">The db context value.</param>
    protected CrudController(TDbContext dbContext)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Gets db context.
    /// </summary>
    protected TDbContext DbContext { get; }

    /// <summary>
    /// Gets or sets entities.
    /// </summary>
    protected virtual DbSet<TEntity> Entities => DbContext.Set<TEntity>();

    /// <summary>
    /// Gets or sets default page size.
    /// </summary>
    protected virtual int DefaultPageSize => 25;

    /// <summary>
    /// Gets or sets max page size.
    /// </summary>
    protected virtual int MaxPageSize => 100;

    /// <summary>
    /// Gets or sets max patch operations.
    /// </summary>
    protected virtual int MaxPatchOperations => 100;
}
