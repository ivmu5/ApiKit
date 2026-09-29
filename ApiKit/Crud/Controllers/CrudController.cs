using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiKit.Crud.Controllers;

/// <summary>
/// Базовый ASP.NET Core API-контроллер с типовыми CRUD-операциями поверх EF Core.
/// </summary>
/// <typeparam name="TDbContext">Тип контекста EF Core.</typeparam>
/// <typeparam name="TEntity">Тип EF Core entity.</typeparam>
/// <typeparam name="TKey">Тип единственного первичного ключа ресурса.</typeparam>
/// <typeparam name="TReadModel">Модель, возвращаемая клиенту.</typeparam>
/// <typeparam name="TCreateModel">Модель создания ресурса.</typeparam>
/// <typeparam name="TUpdateModel">Модель PUT/PATCH-обновления ресурса.</typeparam>
/// <remarks>
/// Контроллер использует стандартные механизмы ASP.NET Core MVC и EF Core и не вводит
/// repository, unit of work или собственный язык запросов. HTTP actions и основные
/// этапы обработки объявлены <see langword="virtual"/> и могут быть переопределены.
/// </remarks>
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
    /// Создаёт CRUD-контроллер для указанного контекста EF Core.
    /// </summary>
    /// <param name="dbContext">Контекст EF Core.</param>
    protected CrudController(TDbContext dbContext)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Контекст EF Core текущего запроса.
    /// </summary>
    protected TDbContext DbContext { get; }

    /// <summary>
    /// Набор entity, используемый стандартными CRUD-операциями.
    /// </summary>
    protected virtual DbSet<TEntity> Entities => DbContext.Set<TEntity>();

    /// <summary>
    /// Размер страницы по умолчанию.
    /// </summary>
    protected virtual int DefaultPageSize => 25;

    /// <summary>
    /// Максимальный размер страницы стандартного collection endpoint.
    /// </summary>
    protected virtual int MaxPageSize => 100;

    /// <summary>
    /// Максимальное количество операций в одном JSON Patch документе.
    /// </summary>
    protected virtual int MaxPatchOperations => 100;
}
