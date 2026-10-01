using System.ComponentModel.DataAnnotations;

namespace ApiKit.Crud.Models;

/// <summary>
/// Defines configuration settings for CRUD page options.
/// </summary>
public class CrudPageOptions
{
    /// <summary>
    /// Gets or sets page.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets page size.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? PageSize { get; set; }
}
