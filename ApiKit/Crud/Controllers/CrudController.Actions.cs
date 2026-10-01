using ApiKit.Crud.Attributes;
using ApiKit.Crud.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Mvc;
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
    /// Returns a paginated list of resource read models.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The requested value.</returns>
    [HttpGet]
    [CrudOperation(CrudOperationKind.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public virtual async Task<ActionResult<PagedResult<TReadModel>>> GetAll(
        [FromQuery] CrudPageOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        var pageSize = ResolvePageSize(options);

        if (pageSize > MaxPageSize)
        {
            ModelState.AddModelError(
                nameof(options.PageSize),
                $"Размер страницы не может превышать {MaxPageSize}.");

            return ValidationProblem(ModelState);
        }

        var offset = ((long)options.Page - 1) * pageSize;
        if (offset > int.MaxValue)
        {
            ModelState.AddModelError(
                nameof(options.Page),
                "Номер страницы выходит за диапазон стандартной offset-пагинации.");

            return ValidationProblem(ModelState);
        }

        var query = CreateListQuery(options);
        var totalCount = await query.LongCountAsync(cancellationToken);
        query = ApplyListOrdering(query, options);
        query = ApplyPagination(query, options.Page, pageSize);

        var items = await MaterializeReadModelsAsync(query, cancellationToken);

        return Ok(new PagedResult<TReadModel>(
            items,
            options.Page,
            pageSize,
            totalCount));
    }

    /// <summary>
    /// Returns the resource matching the specified primary key, if present.
    /// </summary>
    /// <param name="id">The primary key of the resource.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The requested value.</returns>
    [HttpGet("{id}")]
    [CrudOperation(CrudOperationKind.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<ActionResult<TReadModel>> GetById(
        [FromRoute] TKey id,
        CancellationToken cancellationToken)
    {
        var entity = await FindReadEntityAsync(id, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        return Ok(await MapReadModelAsync(entity, cancellationToken));
    }

    /// <summary>
    /// Creates and persists a resource and returns its read model.
    /// </summary>
    /// <param name="model">The input data model.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The created value.</returns>
    [HttpPost]
    [CrudOperation(CrudOperationKind.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public virtual async Task<ActionResult<TReadModel>> Create(
        [FromBody] TCreateModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        var entity = await CreateEntityAsync(model, cancellationToken);
        await Entities.AddAsync(entity, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        var readModel = await MapReadModelAsync(entity, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            GetCreatedRouteValues(entity),
            readModel);
    }

    /// <summary>
    /// Replaces the existing resource using the provided update model.
    /// </summary>
    /// <param name="id">The primary key of the resource.</param>
    /// <param name="model">The input data model.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The result of the operation.</returns>
    [HttpPut("{id}")]
    [CrudOperation(CrudOperationKind.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> Update(
        [FromRoute] TKey id,
        [FromBody] TUpdateModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        var entity = await FindEntityAsync(id, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        await ApplyUpdateModelAsync(entity, model, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Partially updates a resource using a validated JSON Patch document.
    /// </summary>
    /// <param name="id">The primary key of the resource.</param>
    /// <param name="patchDocument">The JSON Patch document.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The result of the operation.</returns>
    [HttpPatch("{id}")]
    [Consumes("application/json-patch+json")]
    [CrudOperation(CrudOperationKind.Update)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public virtual async Task<ActionResult<TReadModel>> Patch(
        [FromRoute] TKey id,
        [FromBody] JsonPatchDocument<TUpdateModel> patchDocument,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDocument);

        var entity = await FindEntityAsync(id, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        ValidatePatchDocument(patchDocument);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updateModel = await CreateUpdateModelAsync(entity, cancellationToken);
        patchDocument.ApplyTo(updateModel, ModelState);

        if (!ModelState.IsValid || !TryValidateModel(updateModel))
        {
            return ValidationProblem(ModelState);
        }

        await ApplyUpdateModelAsync(entity, updateModel, cancellationToken);
        await DbContext.SaveChangesAsync(cancellationToken);

        return Ok(await MapReadModelAsync(entity, cancellationToken));
    }

    /// <summary>
    /// Deletes a resource by its primary key.
    /// </summary>
    /// <param name="id">The primary key of the resource.</param>
    /// <param name="cancellationToken">A token that cancels this operation.</param>
    /// <returns>The result of the operation.</returns>
    [HttpDelete("{id}")]
    [CrudOperation(CrudOperationKind.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> Delete(
        [FromRoute] TKey id,
        CancellationToken cancellationToken)
    {
        var entity = await FindEntityAsync(id, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        Entities.Remove(entity);
        await DbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
